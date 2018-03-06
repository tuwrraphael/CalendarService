namespace CalendarService
{
    public class CalendarConfigurationOptions
    {
        public string MSClientId { get; set; }
        public string MSRedirectUri { get; set; }
        public string MSSecret { get; set; }
        public string GraphNotificationUri { get; set; }
        public string NotificationMaintainanceUri { get; set; }
    }
}