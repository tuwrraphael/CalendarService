using System;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarService
{
    public class CalendarService : ICalendarService
    {
        private readonly IConfigurationRepository configurationRepository;
        private readonly IGraphCalendarProviderFactory graphCalendarProviderFactory;

        public CalendarService(IConfigurationRepository configurationRepository, IGraphCalendarProviderFactory graphCalendarProviderFactory)
        {
            this.configurationRepository = configurationRepository;
            this.graphCalendarProviderFactory = graphCalendarProviderFactory;
        }

        public async Task<Event[]> Get(string userId, DateTime from, DateTime to)
        {
            var configs = await configurationRepository.GetConfigurations(userId);
            if (null == configs || 0 == configs.Length)
            {
                return null;
            }
            var providers = configs.Select(config =>
            {
                switch (config.Type)
                {
                    case CalendarType.Microsoft:
                        return graphCalendarProviderFactory.GetProvider(config);
                    default:
                        throw new NotImplementedException();
                }
            });
            var eventTasks = providers.Select(v => v.Get(from, to)).ToArray();
            var res = await Task.WhenAll(eventTasks);
            return res.SelectMany(v => v).OrderBy(v => v.Start).ToArray();
        }
    }
}
