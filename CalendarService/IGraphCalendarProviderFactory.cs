namespace CalendarService
{
    public interface IGraphCalendarProviderFactory
    {
        ICalendarProvider GetProvider(StoredConfiguration config);
    }
}