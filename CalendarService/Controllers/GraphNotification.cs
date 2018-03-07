using System;

namespace CalendarService.Controllers
{
    public class GraphNotification
    {
        public string SubscriptionId { get; set; }
        public DateTime SubscriptionExpirationDateTime { get; set; }
        public string ClientState { get; set; }
        public string ChangeType { get; set; }
        public string Resource { get; set; }
    }
}
