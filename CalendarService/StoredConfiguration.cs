using System;
using System.Collections.Generic;

namespace CalendarService
{
    public class StoredConfiguration
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresIn { get; set; }
        public List<StoredFeed> SubscribedFeeds { get; set; }
    }
}