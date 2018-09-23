using System.Threading.Tasks;
using Google.Apis.Http;

namespace CalendarService
{
    public interface IGoogleCredentialProvider
    {
        Task<IConfigurableHttpClientInitializer> CreateByConfigAsync(StoredConfiguration config);
    }
}