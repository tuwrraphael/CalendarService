namespace CalendarService.Models
{
    public class LocationData
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public GeoCoordinate Coordinate { get; set; }
        public AddressData Address { get; set; }
    }
}