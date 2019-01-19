using System.Security.Cryptography;
using System.Text;

namespace CalendarService.Models
{
    public static class EventExtensions
    {
        public static string GenerateHash(this Event evt)
        {
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes($"{evt.Start:o}+{LocationHashString(evt.Location)}"));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string GetFormattedAddress(this Event evt)
        {
            return GetFormattedAddress(evt.Location);
        }

        internal static string LocationHashString(LocationData data)
        {
            return GetFormattedAddress(data) ?? string.Empty;
        }

        internal static string GetFormattedAddress(LocationData locationData)
        {
            return locationData?.Address != null ?
                $"{locationData.Address.Street}, {locationData.Address.PostalCode} {locationData.Address.City} {locationData.Address.CountryOrRegion}" :
                locationData?.Text != null ?
                    locationData.Text :
                    locationData?.Coordinate != null ?
                        $"{locationData.Coordinate.Latitude}|{locationData.Coordinate.Longitude}" :
                        null;
        }
    }
}
