using System.Collections.Generic;

namespace CalendarService
{
    public class User
    {
        public List<StoredToken> Tokens { get; set; }
        public string Id { get; set; }
        public List<StoredConfigState> ConfigStates { get; set; }
    }
}