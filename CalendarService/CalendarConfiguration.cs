namespace CalendarService
{
    public class CalendarConfiguration
    {
        public string Id { get; set; }
        public CalendarType Type { get; set; }
        public string Identifier { get; set; }
        public Feed[] Feeds { get; set; }
    }
}