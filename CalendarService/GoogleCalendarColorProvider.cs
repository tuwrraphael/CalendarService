using System;
using System.Threading;
using System.Threading.Tasks;
using CalendarService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CalendarService
{
    public class GoogleCalendarColorProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly StoredConfiguration _config;
        private readonly IGoogleCredentialProvider _googleCredentialProvider;
        private readonly ILogger<GoogleCalendarColorProvider> _logger;
        private Google.Apis.Calendar.v3.Data.Colors _colors;
        private static SemaphoreSlim _colorsSem = new SemaphoreSlim(1);
        private static SemaphoreSlim _calendarSem = new SemaphoreSlim(1);

        public GoogleCalendarColorProvider(IMemoryCache memoryCache,
            StoredConfiguration config,
            IGoogleCredentialProvider googleCredentialProvider,
            ILogger<GoogleCalendarColorProvider> logger)
        {
            _memoryCache = memoryCache;
            _config = config;
            _googleCredentialProvider = googleCredentialProvider;
            _logger = logger;
        }

        public async Task<EventCategory> GetCategory(Google.Apis.Calendar.v3.Data.Event e,
             string feedId)
        {
            var colors = await GetColors();
            if (null != e.ColorId)
            {
                if (colors.Event__.TryGetValue(e.ColorId, out Google.Apis.Calendar.v3.Data.ColorDefinition eventColor))
                {
                    return new EventCategory()
                    {
                        Background = eventColor.Background,
                        Foreground = eventColor.Foreground,
                        Name = e.ColorId
                    };
                }
            }
            var calendarListEntry = await GetCalendarListEntry(feedId);
            if (null != calendarListEntry?.ColorId)
            {
                if (colors.Calendar.TryGetValue(calendarListEntry.ColorId, out Google.Apis.Calendar.v3.Data.ColorDefinition feedColor))
                {
                    return new EventCategory()
                    {
                        Background = feedColor.Background,
                        Foreground = feedColor.Foreground,
                        Name = null
                    };
                }
            }
            return null;
        }

        private async Task<Google.Apis.Calendar.v3.Data.CalendarListEntry> GetCalendarListEntry(string feedId)
        {
            try
            {
                await _calendarSem.WaitAsync();

                if (_memoryCache.TryGetValue($"calendar:{_config.Id}:{feedId}", out
                    Google.Apis.Calendar.v3.Data.CalendarListEntry entry))
                {
                    return entry;
                }
                var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = await _googleCredentialProvider.CreateByConfigAsync(_config)
                });
                try
                {
                    entry = await client.CalendarList.Get(feedId).ExecuteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not retrieve entry for {_config.Id}:{feedId}");
                    return null;
                }
                _memoryCache.Set($"calendar:{_config.Id}:{feedId}", entry, new TimeSpan(0, 20, 0));
                return entry;
            }
            finally
            {
                _calendarSem.Release();
            }
        }

        private async Task<Google.Apis.Calendar.v3.Data.Colors> GetColors()
        {
            try
            {
                await _colorsSem.WaitAsync();
                if (null != _colors)
                {
                    return _colors;
                }
                if (_memoryCache.TryGetValue($"colors:{_config.Id}", out
                    Google.Apis.Calendar.v3.Data.Colors colors))
                {
                    _colors = colors;
                    return colors;
                }
                var client = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = await _googleCredentialProvider.CreateByConfigAsync(_config)
                });
                colors = await client.Colors.Get().ExecuteAsync();
                _memoryCache.Set($"colors:{_config.Id}", colors, new TimeSpan(0, 20, 0));
                _colors = colors;
                return colors;
            }
            finally
            {
                _colorsSem.Release();
            }
        }

    }
}