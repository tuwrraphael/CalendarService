using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CalendarService
{
    public class GoogleCalendarColorProviderFactory : IGoogleCalendarColorProviderFactory
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<GoogleCalendarColorProvider> _logger;

        public GoogleCalendarColorProviderFactory(IMemoryCache memoryCache,
            ILogger<GoogleCalendarColorProvider> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public GoogleCalendarColorProvider Get(StoredConfiguration config,
            IGoogleCredentialProvider googleCredentialProvider)
        {
            return new GoogleCalendarColorProvider(_memoryCache,
                config, googleCredentialProvider, _logger);
        }
    }
}