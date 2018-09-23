using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Json;
using Google.Apis.Requests;
using Google.Apis.Requests.Parameters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarService
{
    public class CalendarConfigurationsService : ICalendarConfigurationService
    {
        private readonly IConfigurationRepository repository;
        private readonly IGraphAuthenticationProviderFactory authenticationProviderFactory;
        private readonly IGoogleCredentialProvider googleCredentialProvider;
        private readonly ILogger logger;
        private readonly CalendarConfigurationOptions options;

        public CalendarConfigurationsService(IConfigurationRepository repository,
            IOptions<CalendarConfigurationOptions> optionsAccessor,
            IGraphAuthenticationProviderFactory authenticationProviderFactory,
            ILoggerFactory logger,
            IGoogleCredentialProvider googleCredentialProvider)
        {
            this.repository = repository;
            this.authenticationProviderFactory = authenticationProviderFactory;
            this.googleCredentialProvider = googleCredentialProvider;
            this.logger = logger.CreateLogger("CalendarConfigurationsService");
            options = optionsAccessor.Value;
        }

        public async Task<CalendarConfiguration[]> GetConfigurations(string userid)
        {
            var storedConfigurations = await repository.GetConfigurations(userid);
            var res = new List<CalendarConfiguration>();
            if (null == storedConfigurations)
            {
                return null;
            }
            foreach (var config in storedConfigurations)
            {
                if (config.Type == CalendarType.Microsoft)
                {
                    Feed[] feeds = new Feed[0];
                    var provider = await authenticationProviderFactory.GetByConfig(config);
                    var client = new GraphServiceClient(provider);
                    try
                    {
                        var calendars = await client.Me.Calendars.Request().GetAsync();
                        feeds = calendars.Select(calendar => new Feed()
                        {
                            Id = calendar.Id,
                            Name = calendar.Name,
                            Subscribed = config.SubscribedFeeds.Any(v => v.FeedId == calendar.Id)
                        }).ToArray();
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Could not retrieve feeds from MSGraph");
                    }
                    res.Add(new CalendarConfiguration()
                    {
                        Type = config.Type,
                        Feeds = feeds,
                        Id = config.Id,
                        Identifier = "MS Calendar"
                    });
                }
                else if (config.Type == CalendarType.Google)
                {
                    Feed[] feeds = new Feed[0];
                    var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
                    {
                        HttpClientInitializer = await googleCredentialProvider.CreateByConfigAsync(config)
                    });
                    try
                    {
                        var calendars = await client.CalendarList.List().ExecuteAsync();
                        feeds = calendars.Items.Select(calendar => new Feed()
                        {
                            Id = calendar.Id,
                            Name = calendar.Summary,
                            Subscribed = config.SubscribedFeeds.Any(v => v.FeedId == calendar.Id)
                        }).ToArray();
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Could not retrieve feeds from Google");
                    }
                    res.Add(new CalendarConfiguration()
                    {
                        Type = config.Type,
                        Feeds = feeds,
                        Id = config.Id,
                        Identifier = "Google"
                    });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return res.ToArray();
        }

        public async Task<string> GetGoogleLinkUrl(string userId, string redirectUri)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer()
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets()
                {
                    ClientId = options.GoogleClientID,
                    ClientSecret = options.GoogleClientSecret
                },
                Scopes = new[] { "https://www.googleapis.com/auth/calendar.events.readonly",
                    "https://www.googleapis.com/auth/calendar.readonly" }
            });
            var request = flow.CreateAuthorizationCodeRequest(options.GoogleRedirectUri);
            var state = Guid.NewGuid().ToString();
            request.State = state;
            await repository.CreateConfigState(userId, state, redirectUri);
            return request.Build().AbsoluteUri;
        }

        public async Task<string> GetMicrosoftLinkUrl(string userId, string redirectUri)
        {
            var state = Guid.NewGuid().ToString();
            await repository.CreateConfigState(userId, state, redirectUri);
            return $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" +
                $"client_id={options.MSClientId}&response_type=code&" +
                $"redirect_uri={options.MSRedirectUri}&response_mode=query&" +
                $"scope=offline_access%20Calendars.Read&" +
                $"state={state}";
        }

        public async Task<LinkResult> LinkGoogle(string state, string code)
        {
            var configState = await repository.GetConfigState(state);
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer()
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets()
                {
                    ClientId = options.GoogleClientID,
                    ClientSecret = options.GoogleClientSecret
                },
                Scopes = new[] { "https://www.googleapis.com/auth/calendar.events.readonly",
                    "https://www.googleapis.com/auth/calendar.readonly" },
            });
            Google.Apis.Auth.OAuth2.Responses.TokenResponse tokenResponse;
            try
            {
                tokenResponse = await flow.ExchangeCodeForTokenAsync(null, code, options.GoogleRedirectUri, CancellationToken.None);
            }
            catch (TokenResponseException e)
            {
                throw new CalendarConfigurationException($"Google: Could not redeem authorization code: {e.Error.ErrorDescription}", e);
            }
            var id = await repository.AddGoogleTokens(configState.UserId, new TokenResponse()
            {
                access_token = tokenResponse.AccessToken,
                refresh_token = tokenResponse.RefreshToken,
                expires_in = (int)tokenResponse.ExpiresInSeconds.Value
            });
            return new LinkResult()
            {
                RedirectUri = configState.RedirectUri,
                Id = id
            };
        }

        public async Task<LinkResult> LinkMicrosoft(string state, string code)
        {
            var configState = await repository.GetConfigState(state);
            if (null == configState)
            {
                throw new AuthorizationStateNotFoundException();
            }
            var client = new HttpClient();
            var dict = new Dictionary<string, string>() {
                { "client_id", options.MSClientId},
                { "scope", "offline_access Calendars.Read"},
                {"grant_type", "authorization_code" },
                {"code", code },
                {"redirect_uri", options.MSRedirectUri},
                {"client_secret", options.MSSecret }
            };
            var result = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token",
                new FormUrlEncodedContent(dict));
            if (!result.IsSuccessStatusCode)
            {
                throw new CalendarConfigurationException($"MS: Could not redeem authorization code: {result.StatusCode}");
            }
            var tokenResponse = await result.Content.ReadAsStringAsync();
            var tokens = JsonConvert.DeserializeObject<TokenResponse>(tokenResponse);
            var id = await repository.AddMicrosoftTokens(configState.UserId, tokens);
            return new LinkResult()
            {
                RedirectUri = configState.RedirectUri,
                Id = id
            };
        }

        public async Task<bool> RemoveConfig(string userId, string configId)
        {
            var config = await repository.GetConfiguration(configId);
            if (config.Type == CalendarType.Google)
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer()
                {
                    ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets()
                    {
                        ClientId = options.GoogleClientID,
                        ClientSecret = options.GoogleClientSecret
                    },
                    Scopes = new[] { "https://www.googleapis.com/auth/calendar.events.readonly",
                    "https://www.googleapis.com/auth/calendar.readonly" },
                    RevokeTokenUrl = "https://accounts.google.com/o/oauth2/revoke"
                });
                try
                {
                    await flow.RevokeTokenAsync(null, config.AccessToken, CancellationToken.None);
                }
                catch (Exception e)
                {
                    logger.LogError("Could not revoke access token.", e);
                    try
                    {
                        await flow.RevokeTokenAsync(null, config.RefreshToken, CancellationToken.None);
                    }
                    catch (Exception e2)
                    {
                        logger.LogError("Could not revoke refresh token.", e2);
                    }
                }
            }
            return await repository.RemoveConfig(userId, configId);
        }

        public async Task<bool> SetFeeds(string v, string id, string[] feedIds)
        {
            return await repository.SetFeeds(v, id, feedIds);
        }
    }
}
