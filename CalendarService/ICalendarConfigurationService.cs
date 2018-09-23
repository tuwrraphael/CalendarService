using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarConfigurationService
    {
        Task<string> GetMicrosoftLinkUrl(string userId, string redirectUri);
        Task<string> GetGoogleLinkUrl(string userId, string redirectUri);

        Task<LinkResult> LinkMicrosoft(string state, string code);

        Task<LinkResult> LinkGoogle(string state, string code);

        Task<bool> RemoveConfig(string userId, string configId);
        Task<CalendarConfiguration[]> GetConfigurations(string v);
        Task<bool> SetFeeds(string v, string id, string[] feedIds);
    }
}
