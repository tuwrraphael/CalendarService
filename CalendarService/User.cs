using System.Collections.Generic;

namespace CalendarService
{
    public class User
    {
        public List<StoredConfiguration> Configurations { get; set; }
        public string Id { get; set; }
        public List<StoredConfigState> ConfigStates { get; set; }
        public List<StoredReminder> Reminders { get; set; }
    }
}