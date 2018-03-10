using ButlerClient;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarService
{
    public class CalendarService : ICalendarService
    {
        private const double RenewNotificationOn = 0.9;

        private readonly IConfigurationRepository configurationRepository;
        private readonly IGraphCalendarProviderFactory graphCalendarProviderFactory;
        private readonly IButler butler;
        private readonly CalendarConfigurationOptions options;

        public CalendarService(IConfigurationRepository configurationRepository,
            IGraphCalendarProviderFactory graphCalendarProviderFactory,
            IButler butler,
            IOptions<CalendarConfigurationOptions> optionsAccessor)
        {
            this.configurationRepository = configurationRepository;
            this.graphCalendarProviderFactory = graphCalendarProviderFactory;
            this.butler = butler;
            options = optionsAccessor.Value;
        }

        private ICalendarProvider GetProvider(StoredConfiguration config)
        {
            switch (config.Type)
            {
                case CalendarType.Microsoft:
                    return graphCalendarProviderFactory.GetProvider(config);
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<Event[]> Get(string userId, DateTime from, DateTime to)
        {
            var configs = await configurationRepository.GetConfigurations(userId);
            if (null == configs || 0 == configs.Length)
            {
                return null;
            }
            var eventTasks = configs.Select(GetProvider).Select(v => v.Get(from, to)).ToArray();
            var res = await Task.WhenAll(eventTasks);
            return res.SelectMany(v => v).OrderBy(v => v.Start).ToArray();
        }

        private async Task InstallButlerForExpiration(string configId, string feedId, DateTime expires)
        {
            var butlerTime = DateTime.Now.AddMilliseconds((expires - DateTime.Now).TotalMilliseconds * RenewNotificationOn);
            await butler.InstallAsync(new WebhookRequest()
            {
                Data = new NotificationMaintainanceRequest()
                {
                    ConfigurationId = configId,
                    FeedId = feedId
                },
                Url = options.NotificationMaintainanceUri,
                When = butlerTime
            });
        }

        public async Task InstallNotifications(string userId)
        {
            var configs = await configurationRepository.GetConfigurations(userId);
            foreach (var config in configs)
            {
                foreach (var feed in config.SubscribedFeeds)
                {
                    if (null == feed.Notification)
                    {
                        var provider = GetProvider(config);
                        var result = await provider.InstallNotification(feed.FeedId);
                        await configurationRepository.UpdateNotification(config.Id, feed.Id, result);
                        await InstallButlerForExpiration(config.Id, feed.Id, result.Expires);
                    }
                }
            }
        }

        public async Task<bool> MaintainNotification(NotificationMaintainanceRequest request)
        {
            var config = await configurationRepository.GetConfiguration(request.ConfigurationId);
            if (null == config) return false;
            var feed = config.SubscribedFeeds.Where(v => v.Id == request.FeedId).SingleOrDefault();
            if (null == feed) return false;
            var provider = GetProvider(config);
            var result = await provider.MaintainNotification(new NotificationInstallation()
            {
                Expires = feed.Notification.Expires,
                NotificationId = feed.Notification.NotificationId,
                ProviderNotifiactionId = feed.Notification.ProviderNotificationId
            });
            var butlerTime = DateTime.Now.AddMilliseconds((result.Expires - DateTime.Now).TotalMilliseconds * RenewNotificationOn);
            await InstallButlerForExpiration(config.Id, feed.Id, result.Expires);
            return true;
        }

        public async Task<string> GetUserIdByNotificationAsync(string notificationId)
        {
            return await configurationRepository.GetUserIdByNotificationAsync(notificationId);
        }
    }
}
