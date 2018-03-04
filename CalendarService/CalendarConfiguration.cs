namespace CalendarService
{
    public class CalendarConfiguration
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Identifier { get; set; }
        public Feed[] Feeds { get; set; }
    }
}