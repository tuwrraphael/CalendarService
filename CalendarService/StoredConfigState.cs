using System;

namespace CalendarService
{
    public class StoredConfigState
    {
        public string UserId { get; set; }
        public string RedirectUri { get; set; }
        public string State { get; set; }
        public User User { get; set; }
        public DateTime StoredTime { get; set; }
    }
}