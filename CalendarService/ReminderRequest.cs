using System.ComponentModel.DataAnnotations;

namespace CalendarService
{
    public class ReminderRequest
    {
        [Required]
        public uint Minutes { get; set; }
        [Required]
        public string NotificationUri { get; set; }
        [Required]
        public string ClientState { get; set; }
    }
}