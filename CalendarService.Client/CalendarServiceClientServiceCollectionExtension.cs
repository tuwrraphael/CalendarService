using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OAuthApiClient;
using System;

namespace CalendarService.Client
{
    public static class CalendarServiceClientServiceCollectionExtension
    {
        public static void AddCalendarServiceClient(this IServiceCollection services,
            Uri baseUri,
            IAuthenticationProviderBuilder authenticationProviderBuilder)
        {
            var factory = authenticationProviderBuilder.GetFactory();
            services.Configure<CalendarServiceOptions>(v => v.CalendarServiceBaseUri = baseUri);
            services.AddTransient<ICalendarServiceClient>(v => new CalendarServiceClient(factory(v), v.GetService<IOptions<CalendarServiceOptions>>()));
        }
    }
}
