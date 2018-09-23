using Microsoft.Extensions.Options;

namespace CalendarService
{
    public class GoogleCalendarProviderFactory : IGoogleCalendarProviderFactory
    {
        private readonly IGoogleCredentialProvider googleCredentialProvider;
        private readonly IOptions<CalendarConfigurationOptions> options;

        public GoogleCalendarProviderFactory(IGoogleCredentialProvider googleCredentialProvider,
            IOptions<CalendarConfigurationOptions> options)
        {
            this.googleCredentialProvider = googleCredentialProvider;
            this.options = options;
        }

        public ICalendarProvider GetProvider(StoredConfiguration config)
        {
            return new GoogleCalendarProvider(config, googleCredentialProvider, options);
        }
    }
}