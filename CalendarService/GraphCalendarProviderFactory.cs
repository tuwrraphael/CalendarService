namespace CalendarService
{
    public class GraphCalendarProviderFactory : IGraphCalendarProviderFactory
    {
        private readonly IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory;

        public GraphCalendarProviderFactory(IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory)
        {
            this.graphAuthenticationProviderFactory = graphAuthenticationProviderFactory;
        }

        public ICalendarProvider GetProvider(StoredConfiguration config)
        {
            return new GraphCalendarProvider(graphAuthenticationProviderFactory, config);
        }
    }
}
