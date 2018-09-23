using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace CalendarService
{
    public class GraphTokenAuthenticationProvider : IAuthenticationProvider
    {
        public GraphTokenAuthenticationProvider(IConfigurationRepository configurationRepository, CalendarConfigurationOptions options, StoredConfiguration config)
        {
            this.configurationRepository = configurationRepository;
            this.options = options;
            this.config = config;
        }

        private StoredConfiguration config;
        private readonly IConfigurationRepository configurationRepository;
        private readonly CalendarConfigurationOptions options;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var semaphore = ConfigurationRepository.ConfigSemaphores.GetOrAdd(config.Id, new SemaphoreSlim(1));
            await semaphore.WaitAsync(); //if the authenticationprovider is used concurrently, only the first parallel usage shall refresh the token
            try
            {
                if (config.ExpiresIn <= DateTime.Now)
                {
                    var client = new HttpClient();
                    var dict = new Dictionary<string, string>() {
                { "client_id", options.MSClientId},
                { "scope", "offline_access Calendars.Read"},
                {"grant_type", "refresh_token" },
                {"refresh_token", config.RefreshToken},
                {"redirect_uri", options.MSRedirectUri},
                {"client_secret", options.MSSecret }
            };
                    var result = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token",
                        new FormUrlEncodedContent(dict));
                    if (!result.IsSuccessStatusCode)
                    {
                        throw new CalendarConfigurationException($"Could not redeem refresh token: {result.StatusCode}");
                    }
                    var tokenResponse = await result.Content.ReadAsStringAsync();
                    var tokens = JsonConvert.DeserializeObject<TokenResponse>(tokenResponse);
                    config = await configurationRepository.RefreshTokens(config.Id, tokens);
                }
            }
            finally
            {
                semaphore.Release();
            }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
        }
    }
}