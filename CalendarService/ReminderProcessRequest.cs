namespace CalendarService
{
    public class ReminderProcessRequest
    {
        public string InstanceId { get; set; }
        public string ReminderId { get; set; }
        public string Hash { get; set; }
    }
}