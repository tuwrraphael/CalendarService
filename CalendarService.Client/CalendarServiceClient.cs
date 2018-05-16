using CalendarService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OAuthApiClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalendarService.Client
{
    public class CalendarServiceClient : ICalendarServiceClient
    {
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly CalendarServiceOptions options;

        public CalendarServiceClient(IAuthenticationProvider authenticationProvider, IOptions<CalendarServiceOptions> optionsAccessor)
        {
            this.authenticationProvider = authenticationProvider;
            options = optionsAccessor.Value;
        }

        private async Task<HttpClient> GetClientAsync()
        {
            var client = new HttpClient
            {
                BaseAddress = options.CalendarServiceBaseUri
            };
            await authenticationProvider.AuthenticateClient(client);
            return client;
        }

        public async Task<Event> GetCurrentEventAsync(string userId)
        {
            var res = await (await GetClientAsync()).GetAsync($"api/calendar/{userId}/current");
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Event>(content);
            }
            else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            throw new CalendarServiceException($"Could not retrieve current event: {res.StatusCode}");
        }

        public async Task<ReminderRegistration> RegisterReminderAsync(string userId, ReminderRequest registration)
        {
            var res = await (await GetClientAsync()).PostAsync($"api/users/{userId}/reminders",
                new StringContent(
                    JsonConvert.SerializeObject(registration), Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ReminderRegistration>(await res.Content.ReadAsStringAsync());
            }
            throw new CalendarServiceException($"Could not register reminder: {res.StatusCode}.");
        }

        public async Task<bool> ReminderAliveAsync(string userId, string reminderId)
        {
            var res = await (await GetClientAsync()).GetAsync($"api/users/{userId}/reminders/{reminderId}");
            if (res.IsSuccessStatusCode)
            {
                return true;
            }
            else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            throw new CalendarServiceException($"Could retrieve reminder status: {res.StatusCode}.");
        }

        public async Task<ReminderRegistration> RenewReminderAsync(string userId, string id)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                $"api/users/{userId}/reminders/{id}");
            var res = await (await GetClientAsync()).SendAsync(request);
            if (!res.IsSuccessStatusCode)
            {
                throw new CalendarServiceException($"Could not renew reminder: {res.StatusCode}.");
            }
            return JsonConvert.DeserializeObject<ReminderRegistration>(await res.Content.ReadAsStringAsync());
        }
    }
}
