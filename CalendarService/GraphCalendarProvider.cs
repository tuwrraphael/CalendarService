using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarService
{
    public class GraphCalendarProvider : ICalendarProvider
    {
        private readonly IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory;
        private readonly StoredConfiguration config;
        private IAuthenticationProvider authenticationProvider;

        public GraphCalendarProvider(IGraphAuthenticationProviderFactory graphAuthenticationProviderFactory, StoredConfiguration config)
        {
            this.graphAuthenticationProviderFactory = graphAuthenticationProviderFactory;
            this.config = config;
        }

        public async Task<Event[]> Get(DateTime from, DateTime to)
        {
            authenticationProvider = authenticationProvider ?? await graphAuthenticationProviderFactory.GetByConfig(config);
            var client = new GraphServiceClient(authenticationProvider);
            var options = new[]
            {
                    new QueryOption("startDateTime", from.ToUniversalTime().ToString("o")),
                    new QueryOption("endDateTime", to.ToUniversalTime().ToString("o")),
            };
            var tasks = config.SubscribedFeeds.Select(async v =>
                await client.Me.Calendars[v.FeedId].CalendarView.Request(options).GetAsync());
            var events = await Task.WhenAll(tasks);
            return events.SelectMany(v => v).Select(v => new Event()
            {
                End = DateTime.Parse(v.End.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Start = DateTime.Parse(v.Start.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Subject = v.Subject,
                Location = v.Location.DisplayName,
                IsAllDay = v.IsAllDay.HasValue && v.IsAllDay.Value
            }).ToArray();
        }
    }
}
