using Newtonsoft.Json;
using System;

namespace CalendarService
{
    public class ReminderRegistration
    {
        public DateTime Expires { get; set; }
        public string Id { get; set; }
    }
}