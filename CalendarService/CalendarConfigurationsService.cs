using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CalendarService
{
    public class CalendarConfigurationsService : ICalendarConfigurationService
    {
        private readonly IConfigurationRepository repository;
        private readonly CalendarConfigurationOptions options;

        public CalendarConfigurationsService(IConfigurationRepository repository, IOptions<CalendarConfigurationOptions> optionsAccessor)
        {
            this.repository = repository;
            options = optionsAccessor.Value;
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
    }
}
