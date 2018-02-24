using System.ComponentModel.DataAnnotations;

namespace CalendarService
{
    public class CalendarLinkRequest
    {
        [Required]
        public CalendarType CalendarType { get; set; }
        public string RedirectUri { get; set; }
    }
}
