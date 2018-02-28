using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace CalendarService
{
    public class GraphAuthenticationProviderFactory : IGraphAuthenticationProviderFactory
    {
        private readonly IConfigurationRepository configurationRepository;
        private readonly CalendarConfigurationOptions options;

        public GraphAuthenticationProviderFactory(IConfigurationRepository configurationRepository, IOptions<CalendarConfigurationOptions> optionsAccessor)
        {
            this.configurationRepository = configurationRepository;
            options = optionsAccessor.Value;
        }

        public async Task<IAuthenticationProvider> GetByConfigId(string configId)
        {
            var config = await configurationRepository.GetConfiguration(configId);
            return new GraphTokenAuthenticationProvider(configurationRepository, options, config);
        }

        public async Task<IAuthenticationProvider> GetByConfig(StoredConfiguration config)
        {
            return new GraphTokenAuthenticationProvider(configurationRepository, options, config);
        }
    }
}