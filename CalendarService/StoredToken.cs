using System;

namespace CalendarService
{
    public class StoredToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresIn { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
    }
}