namespace CalendarService
{
    public class CalendarConfigurationOptions
    {
        public string MSClientId { get; set; }
        public string MSRedirectUri { get; set; }
        public string MSSecret { get; set; }
        public string GraphNotificationUri { get; set; }
        public string NotificationMaintainanceUri { get; set; }
        public string MaintainRemindersUri { get; set; }
        public string ProcessReminderUri { get; set; }
        public string GoogleClientSecret { get; set; }
        public string GoogleClientID { get; set; }
        public string GoogleRedirectUri { get; set; }
        public string GoogleNotificationUri { get; set; }
    }
}