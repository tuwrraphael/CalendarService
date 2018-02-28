using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CalendarService
{
    public class CalendarConfigurationsService : ICalendarConfigurationService
    {
        private readonly IConfigurationRepository repository;
        private readonly IGraphAuthenticationProviderFactory authenticationProviderFactory;
        private readonly CalendarConfigurationOptions options;

        public CalendarConfigurationsService(IConfigurationRepository repository,
            IOptions<CalendarConfigurationOptions> optionsAccessor,
            IGraphAuthenticationProviderFactory authenticationProviderFactory)
        {
            this.repository = repository;
            this.authenticationProviderFactory = authenticationProviderFactory;
            options = optionsAccessor.Value;
        }

        public async Task<CalendarConfiguration[]> GetConfigurations(string userid)
        {
            var storedConfigurations = await repository.GetConfigurations(userid);
            var res = new List<CalendarConfiguration>();
            foreach (var config in storedConfigurations)
            {
                if (config.Type == "microsoft")
                {
                    var provider = await authenticationProviderFactory.GetByConfig(config);
                    var client = new GraphServiceClient(provider);
                    var calendars = await client.Me.Calendars.Request().GetAsync();
                    var feeds = calendars.Select(calendar => new Feed()
                    {
                        Id = calendar.Id,
                        Name = calendar.Name,
                        Subscribed = config.SubscribedFeeds.Any(v => v.FeedId == calendar.Id)
                    }).ToArray();
                    res.Add(new CalendarConfiguration()
                    {
                        Type = CalendarType.Microsoft,
                        Feeds = feeds,
                        Id = config.Id,
                        Identifier = "MS Calendar"
                    });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return res.ToArray();
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

        public Task<LinkResult> LinkGoogle(string state, string code)
        {
            throw new NotImplementedException();
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
                throw new CalendarConfigurationException($"Could not redeem authorization code: {result.StatusCode}");
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
            return await repository.RemoveConfig(userId, configId);
        }

        public async Task<bool> SetFeeds(string v, string id, string[] feedIds)
        {
            return await repository.SetFeeds(v, id, feedIds);
        }
    }
}
