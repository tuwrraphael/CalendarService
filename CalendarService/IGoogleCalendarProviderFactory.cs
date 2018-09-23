namespace CalendarService
{
    public interface IGoogleCalendarProviderFactory
    {
        ICalendarProvider GetProvider(StoredConfiguration config);
    }
}