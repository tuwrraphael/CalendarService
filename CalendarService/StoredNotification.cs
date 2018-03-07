using System;

namespace CalendarService
{
    public class StoredNotification
    {
        public string NotificationId { get; set; }
        public string ProviderNotificationId { get; set; }
        public DateTime Expires { get; set; }
        public StoredFeed Feed { get; set; }
        public string StoredFeedId { get; internal set; }
    }
}