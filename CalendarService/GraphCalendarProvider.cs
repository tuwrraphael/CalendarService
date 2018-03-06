using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ButlerClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace CalendarService
{
    public class GraphCalendarProvider : ICalendarProvider
    {
        private const uint NotificationExpiration = 5;
        private const uint RenewNotificationOn = 2;
        private const uint RenewNotificationThreshold = 4;

        private readonly IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory;
        private readonly StoredConfiguration config;
        private readonly IConfigurationRepository configurationRepository;
        private readonly IButler butler;
        private readonly CalendarConfigurationOptions options;
        private readonly ILogger logger;
        private IAuthenticationProvider authenticationProvider;
        private async Task<IAuthenticationProvider> AuthenticationProviderAsync() => authenticationProvider ?? (authenticationProvider = await graphAuthenticationProviderFactory.GetByConfig(config));

        public GraphCalendarProvider(IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory,
            StoredConfiguration config,
            IOptions<CalendarConfigurationOptions> optionsAccessor,
            ILoggerFactory logger,
            IConfigurationRepository configurationRepository,
            IButler butler)
        {
            this.graphAuthenticationProviderFactory = graphAuthenticationProviderFactory;
            this.config = config;
            this.configurationRepository = configurationRepository;
            this.butler = butler;
            options = optionsAccessor.Value;
            this.logger = logger.CreateLogger("GraphCalendarProvider");
        }

        public async Task<Event[]> Get(DateTime from, DateTime to)
        {
            var client = new GraphServiceClient(await AuthenticationProviderAsync());
            var options = new[]
            {
                    new QueryOption("startDateTime", from.ToUniversalTime().ToString("o")),
                    new QueryOption("endDateTime", to.ToUniversalTime().ToString("o")),
            };
            var tasks = config.SubscribedFeeds.Select(async v =>
                await client.Me.Calendars[v.FeedId].CalendarView.Request(options).GetAsync());
            var events = await Task.WhenAll(tasks);
            return events.SelectMany(v => v).Select(v => new Event()
            {
                End = DateTime.Parse(v.End.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Start = DateTime.Parse(v.Start.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Subject = v.Subject,
                Location = v.Location.DisplayName,
                IsAllDay = v.IsAllDay.HasValue && v.IsAllDay.Value
            }).ToArray();
        }

        public async Task MaintainNotifications()
        {
            var client = new GraphServiceClient(await AuthenticationProviderAsync());
            foreach (var feed in config.SubscribedFeeds)
            {
                if (null == feed.Notification)
                {
                    var notificationId = Guid.NewGuid().ToString();
                    Subscription result;
                    try
                    {
                        result = await client.Subscriptions.Request().AddAsync(new Subscription()
                        {
                            ChangeType = "created,updated,deleted",
                            NotificationUrl = options.GraphNotificationUri,
                            ClientState = notificationId,
                            ExpirationDateTime = DateTime.Now.AddMinutes(NotificationExpiration),
                            Resource = $"/me/calendars/{feed.FeedId}/events"
                        });
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Could not register notification for feed");
                        continue;
                    }
                    var notification = new StoredNotification()
                    {
                        NotificationId = notificationId,
                        ProviderNotificationId = result.Id,
                        Expires = result.ExpirationDateTime.Value.UtcDateTime
                    };
                    await configurationRepository.UpdateNotification(feed.Id, notification);
                    feed.Notification = notification;
                    await butler.InstallAsync(new WebhookRequest()
                    {
                        Data = new NotificationMaintainanceRequest()
                        {
                            ConfigurationId = config.Id
                        },
                        Url = options.NotificationMaintainanceUri,
                        When = result.ExpirationDateTime.Value.DateTime.AddMinutes(-RenewNotificationOn)
                    });
                }
                else if (feed.Notification.Expires >= DateTime.Now.AddMinutes(-RenewNotificationThreshold))
                {
                    Subscription result;
                    try
                    {
                        result = await client.Subscriptions[feed.Notification.ProviderNotificationId].Request()
                            .UpdateAsync(new Subscription()
                            {
                                ExpirationDateTime = DateTime.Now.AddMinutes(NotificationExpiration)
                            });
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Could not register notification for feed");
                        continue;
                    }
                    feed.Notification.Expires = result.ExpirationDateTime.Value.UtcDateTime;
                    await configurationRepository.UpdateNotification(feed.Id, feed.Notification);
                    await butler.InstallAsync(new WebhookRequest()
                    {
                        Data = new NotificationMaintainanceRequest()
                        {
                            ConfigurationId = config.Id
                        },
                        Url = options.NotificationMaintainanceUri,
                        When = DateTime.Now.AddMinutes(RenewNotificationOn)
                    });
                }
            }
        }

        public Task UninstallNotifications()
        {
            throw new NotImplementedException();
        }
    }
}
