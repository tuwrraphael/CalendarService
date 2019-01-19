using System;

namespace CalendarService
{
    public class ReminderInstance
    {
        public string FeedId { get; set; }
        public string EventId { get; set; }
        public string Hash { get; set; }
        public string Id { get; internal set; }
        public StoredReminder Reminder { get; set; }
        public DateTime? RemindRemovalUntil { get; set; }
        public string ReminderId { get; set; }
    }
}