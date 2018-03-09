namespace CalendarService
{
    internal class EventReminder
    {
        public string ReminderId { get; set; }
        public Event Event { get; set; }
        public string ClientState { get; set; }
    }
}