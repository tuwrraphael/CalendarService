using CalendarService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OAuthApiClient.Abstractions;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CalendarService.Client
{
    public class CalendarServiceClient : ICalendarServiceClient
    {
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly CalendarServiceOptions options;

        public IUserCollection Users => new UserCollection(GetClientAsync);

        private class UserCollection : IUserCollection
        {
            private readonly Func<Task<HttpClient>> clientFactory;

            public UserCollection(Func<Task<HttpClient>> clientFactory)
            {
                this.clientFactory = clientFactory;
            }

            public IUser this[string userId] => new UserClient(userId, clientFactory);
        }

        private class ReminderCollection : IReminderCollection
        {
            private readonly string userId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public ReminderCollection(string userId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.clientFactory = clientFactory;
            }

            public IReminder this[string reminderId] => new ReminderClient(userId, reminderId, clientFactory);

            public async Task<ReminderRegistration> RegisterAsync(ReminderRequest request)
            {
                var res = await (await clientFactory()).PostAsync($"api/users/{userId}/reminders",
                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
                if (res.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<ReminderRegistration>(await res.Content.ReadAsStringAsync());
                }
                throw new CalendarServiceException($"Could not register reminder: {res.StatusCode}.");
            }
        }

        private class EventCollection : IEventCollection
        {
            private readonly string userId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public EventCollection(string userId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.clientFactory = clientFactory;
            }

            public async Task<Event[]> Get(DateTimeOffset? from, DateTimeOffset? to)
            {
                var query = HttpUtility.ParseQueryString(string.Empty);
                if (from.HasValue)
                {
                    query["from"] = from.Value.ToString("o");
                }
                if (to.HasValue)
                {
                    query["to"] = to.Value.ToString("o");
                }
                var res = await (await clientFactory()).GetAsync($"api/calendar/{userId}?{query.ToString()}");
                if (res.IsSuccessStatusCode)
                {
                    var content = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Event[]>(content);
                }
                else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw new CalendarServiceException($"Could not retrieve events: {res.StatusCode}");
            }

            public async Task<Event> GetCurrentAsync()
            {
                var res = await (await clientFactory()).GetAsync($"api/calendar/{userId}/current");
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
        }

        private class UserClient : IUser
        {
            private readonly string userId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public UserClient(string userId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.clientFactory = clientFactory;
            }

            public IReminderCollection Reminders => new ReminderCollection(userId, clientFactory);

            public IEventCollection Events => new EventCollection(userId, clientFactory);

            public IFeedCollection Feeds => new FeedCollection(userId, clientFactory);
        }

        private class FeedCollection : IFeedCollection
        {
            private readonly string userId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public FeedCollection(string userId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.clientFactory = clientFactory;
            }

            public IFeed this[string feedId] => new FeedClient(userId, feedId, clientFactory);
        }

        private class FeedClient : IFeed
        {
            private readonly string userId;
            private readonly string feedId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public FeedClient(string userId, string feedId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.feedId = feedId;
                this.clientFactory = clientFactory;
            }

            public IFeedEventCollection Events => new FeedEventCollection(userId, feedId, clientFactory);
        }

        private class FeedEventCollection : IFeedEventCollection
        {
            private readonly string userId;
            private readonly string feedId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public FeedEventCollection(string userId, string feedId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.feedId = feedId;
                this.clientFactory = clientFactory;
            }

            public async Task<Event> Get(string Id)
            {
                var res = await(await clientFactory()).GetAsync($"api/calendar/{userId}/{feedId}/{Id}");
                if (res.IsSuccessStatusCode)
                {
                    var content = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Event>(content);
                }
                else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw new CalendarServiceException($"Could not retrieve events: {res.StatusCode}");
            }
        } 
        private class ReminderClient : IReminder
        {
            private readonly string userId;
            private readonly string reminderId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public ReminderClient(string userId, string reminderId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.reminderId = reminderId;
                this.clientFactory = clientFactory;
            }

            public async Task<bool> IsAliveAsync()
            {
                var res = await (await clientFactory()).GetAsync($"api/users/{userId}/reminders/{reminderId}");
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

            public async Task<ReminderRegistration> RenewAsync()
            {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                $"api/users/{userId}/reminders/{reminderId}");
                var res = await (await clientFactory()).SendAsync(request);
                if (!res.IsSuccessStatusCode)
                {
                    throw new CalendarServiceException($"Could not renew reminder: {res.StatusCode}.");
                }
                return JsonConvert.DeserializeObject<ReminderRegistration>(await res.Content.ReadAsStringAsync());
            }
        }

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
    }
}
