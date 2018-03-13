using Microsoft.Extensions.Options;

namespace CalendarService
{
    public class GraphCalendarProviderFactory : IGraphCalendarProviderFactory
    {
        private readonly IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory;
        private readonly IOptions<CalendarConfigurationOptions> optionsAccessor;

        public GraphCalendarProviderFactory(IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory,
            IOptions<CalendarConfigurationOptions> optionsAccessor)
        {
            this.graphAuthenticationProviderFactory = graphAuthenticationProviderFactory;
            this.optionsAccessor = optionsAccessor;
        }

        public ICalendarProvider GetProvider(StoredConfiguration config)
        {
            return new GraphCalendarProvider(graphAuthenticationProviderFactory, config, optionsAccessor);
        }
    }
}
