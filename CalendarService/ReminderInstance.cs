using System;

namespace CalendarService
{
    public class ReminderInstance
    {
        public string FeedId { get; set; }
        public string EventId { get; set; }
        public DateTime Start { get; set; }
        public int Revision { get; set; }
        public string Id { get; internal set; }
        public StoredReminder Reminder { get; set; }
        public string ReminderId { get; set; }
    }
}