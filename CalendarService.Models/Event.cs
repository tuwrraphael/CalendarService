using System;

namespace CalendarService.Models
{
    public class Event
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Subject { get; set; }
        public LocationData Location { get; set; }
        public bool IsAllDay { get; set; }
        public string Id { get; set; }
        public string FeedId { get;  set; }
    }
}