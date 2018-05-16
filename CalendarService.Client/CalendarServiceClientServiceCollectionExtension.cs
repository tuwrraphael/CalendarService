using Microsoft.Extensions.DependencyInjection;
using OAuthApiClient;
using System;

namespace CalendarService.Client
{
    public class CalendarServiceBuilder
    {
        private readonly IServiceCollection services;

        public CalendarServiceBuilder(IServiceCollection services)
        {
            this.services = services;
        }

        public void AddClientCredentialsAuthentication(ClientCredentialsConfig clientCredentialsConfig)
        {
            services.AddTransient<IAuthenticationProvider<ICalendarServiceClient>>(srv =>
            new BearerTokenAuthenticationProvider<ICalendarServiceClient>(srv.GetService<ITokenStore>(), new ClientCredentialsTokenStrategy(clientCredentialsConfig)));
        }
    }

    public static class CalendarServiceClientServiceCollectionExtension
    {

        public static CalendarServiceBuilder AddCalendarServiceClient(this IServiceCollection services, Uri baseUri)
        {
            services.Configure<CalendarServiceOptions>(v => v.CalendarServiceBaseUri = baseUri);
            services.AddTransient<ITokenStore, MemoryCacheTokenStore>();
            services.AddTransient<ICalendarServiceClient, CalendarServiceClient>();
            return new CalendarServiceBuilder(services);
        }
    }
}
