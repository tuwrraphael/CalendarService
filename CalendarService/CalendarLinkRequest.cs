using System.ComponentModel.DataAnnotations;

namespace CalendarService
{
    public class CalendarLinkRequest
    {
        [Required]
        public string CalendarType { get; set; }
        public string RedirectUri { get; set; }
    }
}
