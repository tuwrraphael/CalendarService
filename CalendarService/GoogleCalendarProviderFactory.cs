using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalendarService
{
    public class GoogleCalendarProviderFactory : IGoogleCalendarProviderFactory
    {
        private readonly IGoogleCredentialProvider googleCredentialProvider;
        private readonly IOptions<CalendarConfigurationOptions> options;
        private readonly ILogger<GoogleCalendarProvider> logger;
        private readonly IGoogleCalendarColorProviderFactory _googleCalendarColorProviderFactory;

        public GoogleCalendarProviderFactory(IGoogleCredentialProvider googleCredentialProvider,
            IOptions<CalendarConfigurationOptions> options, ILogger<GoogleCalendarProvider> logger,
            IGoogleCalendarColorProviderFactory googleCalendarColorProviderFactory)
        {
            this.googleCredentialProvider = googleCredentialProvider;
            this.options = options;
            this.logger = logger;
            _googleCalendarColorProviderFactory = googleCalendarColorProviderFactory;
        }

        public ICalendarProvider GetProvider(StoredConfiguration config)
        {
            return new GoogleCalendarProvider(config, googleCredentialProvider, options, logger,
                _googleCalendarColorProviderFactory.Get(config, googleCredentialProvider));
        }
    }
}