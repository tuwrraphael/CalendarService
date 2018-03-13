using System;

namespace CalendarService
{
    public class NotificationInstallation
    {
        public string NotificationId { get; set; }
        public DateTime Expires { get; set; }
        public string ProviderNotifiactionId { get; set; }
    }
}