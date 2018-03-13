namespace CalendarService
{
    public class StoredFeed
    {
        public string Id { get; set; }
        public string FeedId { get; set; }
        public StoredConfiguration Configuration { get; set; }
        public string ConfigurationId { get; set; }
        public StoredNotification Notification { get; set; }
    }
}