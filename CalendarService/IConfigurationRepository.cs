using System.Threading.Tasks;

namespace CalendarService
{
    public interface IConfigurationRepository
    {
        Task<bool> RemoveConfig(string userId, string configId);
        Task<ConfigState> GetConfigState(string state);
        Task<string> AddMicrosoftTokens(string userId, TokenResponse tokens);
        Task CreateConfigState(string userId, string state, string redirectUri);
    }
}