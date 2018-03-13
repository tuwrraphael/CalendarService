namespace CalendarService
{
    public class ReminderProcessRequest
    {
        public string InstanceId { get; set; }
        public string ReminderId { get; set; }
        public int Revision { get; set; }
    }
}