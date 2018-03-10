using System;
using System.Collections.Generic;

namespace CalendarService
{
    public class StoredReminder
    {
        public List<ReminderInstance> Instances { get; set; }

        public uint Minutes { get; set; }
        public string UserId { get; set; }
        public string NotificationUri { get; set; }
        public string Id { get; set; }
        public string ClientState { get; set; }
        public DateTime Expires { get;  set; }
        public User User { get; internal set; }
    }
}