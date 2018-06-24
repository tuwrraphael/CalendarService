using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ButlerClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using CalendarService.Models;

namespace CalendarService
{
    public class GraphCalendarProvider : ICalendarProvider
    {
        private const uint NotificationExpiration = 4230;//max is 4230

        private readonly IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory;
        private readonly StoredConfiguration config;
        private readonly CalendarConfigurationOptions options;

        private IAuthenticationProvider authenticationProvider;
        private async Task<IAuthenticationProvider> AuthenticationProviderAsync() => authenticationProvider ?? (authenticationProvider = await graphAuthenticationProviderFactory.GetByConfig(config));

        public GraphCalendarProvider(IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory,
            StoredConfiguration config,
            IOptions<CalendarConfigurationOptions> optionsAccessor)
        {
            this.graphAuthenticationProviderFactory = graphAuthenticationProviderFactory;
            this.config = config;
            options = optionsAccessor.Value;
        }

        private Models.Event ToEvent(Microsoft.Graph.Event v, string feedId)
        {
            return new Models.Event()
            {
                End = DateTime.Parse(v.End.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Start = DateTime.Parse(v.Start.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Subject = v.Subject,
                Location = new LocationData()
                {
                    Address = v.Location.Address != null ? new AddressData()
                    {
                        City = v.Location.Address.City,
                        CountryOrRegion = v.Location.Address.CountryOrRegion,
                        PostalCode = v.Location.Address.PostalCode,
                        State = v.Location.Address.State,
                        Street = v.Location.Address.Street
                    } : null,
                    Coordinate = v.Location.Coordinates != null
                    && v.Location.Coordinates.Longitude.HasValue
                    && v.Location.Coordinates.Longitude.HasValue ? new GeoCoordinate()
                    {
                        Latitude = v.Location.Coordinates.Latitude.Value,
                        Longitude = v.Location.Coordinates.Longitude.Value
                    } : null,
                    Id = v.Location.UniqueId,
                    Text = v.Location.DisplayName
                },
                IsAllDay = v.IsAllDay.HasValue && v.IsAllDay.Value,
                Id = v.Id,
                FeedId = feedId
            };
        }

        public async Task<Models.Event[]> Get(DateTime from, DateTime to)
        {
            var client = new GraphServiceClient(await AuthenticationProviderAsync());
            var options = new[]
            {
                    new QueryOption("startDateTime", from.ToUniversalTime().ToString("o")),
                    new QueryOption("endDateTime", to.ToUniversalTime().ToString("o")),
            };
            var tasks = config.SubscribedFeeds.Select(async v => new
            {
                feedId = v.Id,
                events =
                await client.Me.Calendars[v.FeedId].CalendarView.Request(options).GetAsync()
            });
            var events = await Task.WhenAll(tasks);
            return events.Select(a => a.events.Select(v => ToEvent(v, a.feedId))).SelectMany(v => v).ToArray();
        }

        public async Task<NotificationInstallation> InstallNotification(string feedId)
        {
            var client = new GraphServiceClient("https://graph.microsoft.com/beta/", await AuthenticationProviderAsync());
            var notificationId = Guid.NewGuid().ToString();
            var result = await client.Subscriptions.Request().AddAsync(new Subscription()
            {
                ChangeType = "created,updated,deleted",
                NotificationUrl = options.GraphNotificationUri,
                ClientState = notificationId,
                ExpirationDateTime = DateTime.Now.AddMinutes(NotificationExpiration),
                Resource = $"/me/calendars/{feedId}/events"
            });
            return new NotificationInstallation()
            {
                NotificationId = notificationId,
                Expires = result.ExpirationDateTime.Value.UtcDateTime,
                ProviderNotifiactionId = result.Id
            };
        }

        public async Task<NotificationInstallation> MaintainNotification(NotificationInstallation installation)
        {
            var client = new GraphServiceClient("https://graph.microsoft.com/beta/", await AuthenticationProviderAsync());
            var sub = await client.Subscriptions[installation.ProviderNotifiactionId].Request().GetAsync();
            sub.ExpirationDateTime = DateTime.Now.AddMinutes(NotificationExpiration);
            var result = await client.Subscriptions[installation.ProviderNotifiactionId].Request()
                .UpdateAsync(sub);
            return new NotificationInstallation()
            {
                NotificationId = installation.NotificationId,
                Expires = result.ExpirationDateTime.Value.UtcDateTime,
                ProviderNotifiactionId = result.Id
            };
        }

        public async Task<Models.Event> GetAsync(string feedId, string eventId)
        {
            var calendarId = config.SubscribedFeeds.Single(v => v.Id == feedId).FeedId;
            var client = new GraphServiceClient(await AuthenticationProviderAsync());
            var res = await client.Me.Calendars[calendarId].Events[eventId].Request().GetAsync();
            return ToEvent(res, feedId);
        }
    }
}
