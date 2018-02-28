using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarService
{
    public interface IGraphAuthenticationProviderFactory
    {
        Task<IAuthenticationProvider> GetByConfigId(string configId);
        Task<IAuthenticationProvider> GetByConfig(StoredConfiguration config);
    }
}