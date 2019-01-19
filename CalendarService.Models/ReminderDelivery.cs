namespace CalendarService.Models
{
    public class ReminderDelivery
    {
        public string ReminderId { get; set; }
        public Event Event { get; set; }
        public string ClientState { get; set; }
        public string EventId { get; set; }
        public string FeedId { get; set; }
        public bool Removed { get; set; }
    }
}