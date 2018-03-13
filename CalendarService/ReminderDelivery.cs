namespace CalendarService
{
    internal class ReminderDelivery
    {
        public string ReminderId { get; set; }
        public Event Event { get; set; }
        public string ClientState { get; set; }
    }
}