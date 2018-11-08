using System;
using System.Linq;
using System.Threading.Tasks;
using CalendarService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalendarService
{
    public class GoogleCalendarProvider : ICalendarProvider
    {
        private StoredConfiguration config;
        private IGoogleCredentialProvider googleCredentialProvider;
        private readonly ILogger<GoogleCalendarProvider> logger;
        private CalendarConfigurationOptions options;

        private static readonly TimeSpan NotficationExpiration = new TimeSpan(3, 0, 0, 0);

        public GoogleCalendarProvider(StoredConfiguration config, IGoogleCredentialProvider googleCredentialProvider,
            IOptions<CalendarConfigurationOptions> optionsAccessor, ILogger<GoogleCalendarProvider> logger)
        {
            this.config = config;
            this.googleCredentialProvider = googleCredentialProvider;
            this.logger = logger;
            options = optionsAccessor.Value;
        }

        private Event ToEvent(Google.Apis.Calendar.v3.Data.Event v, string feedId)
        {
            if (!v.End.DateTime.HasValue)
            {
                throw new InvalidEventException($"End DateTime of event {v.Summary} was null, raw: {v.End.DateTimeRaw}.");
            }
            if (!v.Start.DateTime.HasValue)
            {
                throw new InvalidEventException($"Start DateTime of event {v.Summary} was null, raw: {v.Start.DateTimeRaw}.");
            }
            return new Event()
            {
                End = v.End.DateTime.Value,
                Start = v.Start.DateTime.Value,
                Subject = v.Summary,
                Location = new LocationData()
                {
                    Text = v.Location
                },
                IsAllDay = false,
                Id = v.Id,
                FeedId = feedId
            };
        }

        public async Task<Event[]> Get(DateTime from, DateTime to)
        {
            var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = await googleCredentialProvider.CreateByConfigAsync(config)
            });
            var tasks = config.SubscribedFeeds.Select(async v =>
            {
                var request = client.Events.List(v.FeedId);
                request.TimeMin = from;
                request.TimeMax = to;
                return new
                {
                    feedId = v.Id,
                    events = await request.ExecuteAsync()
                };
            });
            var events = await Task.WhenAll(tasks);
            return events.Select(a => a.events.Items.Select(v =>
            {
                try
                {
                    return ToEvent(v, a.feedId);
                }
                catch (InvalidEventException e)
                {
                    logger.LogError(e, "Could not parse event.");
                    return null;
                }
            }).Where(v => null != v)).SelectMany(v => v).ToArray();
        }

        public async Task<NotificationInstallation> InstallNotification(string feedId)
        {
            var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = await googleCredentialProvider.CreateByConfigAsync(config)
            });
            var notificationId = Guid.NewGuid().ToString();
            var watch = await client.Events.Watch(new Google.Apis.Calendar.v3.Data.Channel()
            {
                Id = notificationId,
                Token = notificationId,
                Type = "web_hook",
                Address = options.GoogleNotificationUri,
                Expiration = DateTimeOffset.Now.Add(NotficationExpiration).ToUnixTimeMilliseconds()
            }, feedId).ExecuteAsync();
            return new NotificationInstallation()
            {
                NotificationId = notificationId,
                Expires = DateTimeOffset.FromUnixTimeMilliseconds(watch.Expiration.Value).UtcDateTime,
                ProviderNotifiactionId = watch.ResourceId
            };
        }

        public async Task<NotificationInstallation> MaintainNotification(NotificationInstallation installation, string feedId)
        {
            var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = await googleCredentialProvider.CreateByConfigAsync(config)
            });
            try
            {
                var stopResult = await client.Channels.Stop(new Google.Apis.Calendar.v3.Data.Channel()
                {
                    Id = installation.NotificationId,
                    ResourceId = installation.ProviderNotifiactionId
                }).ExecuteAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not delete existing google notification channel.");
            }
            var installed = await InstallNotification(feedId);
            return installed;
        }

        public async Task<Event> GetAsync(string feedId, string eventId)
        {
            var calendarId = config.SubscribedFeeds.Single(v => v.Id == feedId).FeedId;
            var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = await googleCredentialProvider.CreateByConfigAsync(config)
            });
            var res = await client.Events.Get(calendarId, eventId).ExecuteAsync();
            return ToEvent(res, feedId);
        }
    }
}