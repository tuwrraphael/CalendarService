using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Http;
using Microsoft.Extensions.Options;

namespace CalendarService
{
    public class GoogleCredentialProvider : IGoogleCredentialProvider
    {
        private readonly CalendarConfigurationOptions options;
        private readonly IConfigurationRepository configurationRepository;

        public GoogleCredentialProvider(IOptions<CalendarConfigurationOptions> options, IConfigurationRepository configurationRepository)
        {
            this.options = options.Value;
            this.configurationRepository = configurationRepository;
        }
        public async Task<IConfigurableHttpClientInitializer> CreateByConfigAsync(StoredConfiguration config)
        {
            var semaphore = ConfigurationRepository.ConfigSemaphores.GetOrAdd(config.Id, new SemaphoreSlim(1));
            try
            {
                await semaphore.WaitAsync(); //if the authenticationprovider is used concurrently, only the first parallel usage shall refresh the token
                if (config.ExpiresIn <= DateTime.Now)
                {
                    var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer()
                    {
                        Scopes = new[] {"https://www.googleapis.com/auth/calendar.events.readonly",
                    "https://www.googleapis.com/auth/calendar.readonly" },
                        ClientSecrets = new ClientSecrets()
                        {
                            ClientId = options.GoogleClientID,
                            ClientSecret = options.GoogleClientSecret
                        }
                    });
                    Google.Apis.Auth.OAuth2.Responses.TokenResponse tokenResponse;
                    try
                    {
                        tokenResponse = await flow.RefreshTokenAsync("unused", config.RefreshToken, CancellationToken.None);
                    }
                    catch (TokenResponseException e)
                    {
                        throw new CalendarConfigurationException($"Google: Could not redeem authorization code: {e.Error.ErrorDescription}", e);
                    }
                    var tokens = new TokenResponse()
                    {
                        access_token = tokenResponse.AccessToken,
                        expires_in = (int)tokenResponse.ExpiresInSeconds,
                        refresh_token = tokenResponse.RefreshToken ?? config.RefreshToken
                    };
                    config = await configurationRepository.RefreshTokens(config.Id, tokens);
                }
            }
            finally
            {
                semaphore.Release();
            }
            return GoogleCredential.FromAccessToken(config.AccessToken);
        }
    }
}