using System;

namespace CalendarService.Models
{
    public class Event
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string Subject { get; set; }
        public LocationData Location { get; set; }
        public bool IsAllDay { get; set; }
        public string Id { get; set; }
        public string FeedId { get;  set; }
        public EventCategory Category { get; set; }
    }
}