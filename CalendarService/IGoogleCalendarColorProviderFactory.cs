namespace CalendarService
{
    public interface IGoogleCalendarColorProviderFactory
    {
        GoogleCalendarColorProvider Get(StoredConfiguration config, IGoogleCredentialProvider googleCredentialProvider);
    }
}