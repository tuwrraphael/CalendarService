using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ButlerClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalendarService
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            if (string.IsNullOrWhiteSpace(hostingEnvironment.WebRootPath))
            {
                hostingEnvironment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1).AddJsonOptions(v =>
            {
                v.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            });
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddDbContext<CalendarServiceContext>(options =>
                options.UseSqlite($"Data Source={HostingEnvironment.WebRootPath}\\App_Data\\calendarService.db")
            );
            services.AddTransient<IConfigurationRepository, ConfigurationRepository>();
            services.Configure<CalendarConfigurationOptions>(o =>
            {
                o.MSClientId = Configuration["MSClientId"];
                o.MSSecret = Configuration["MSSecret"];
                o.GoogleClientID = Configuration["GoogleClientID"];
                o.GoogleClientSecret = Configuration["GoogleClientSecret"];
                var baseUri = Configuration["CalendarServiceBaseUri"];
                o.MSRedirectUri = $"{baseUri}/api/configuration/ms-connect";
                o.GoogleRedirectUri = $"{baseUri}/api/configuration/google-connect";
                o.GraphNotificationUri = $"{baseUri}/api/callback/graph";
                o.GoogleNotificationUri = $"{baseUri}/api/callback/google";
                o.MaintainRemindersUri = $"{baseUri}/api/callback/reminder-maintainance";
                o.ProcessReminderUri = $"{baseUri}/api/callback/reminder-execute";
                o.NotificationMaintainanceUri = $"{baseUri}/api/callback/notification-maintainance";
            });
            services.AddTransient<ICalendarConfigurationService, CalendarConfigurationsService>();
            services.AddTransient<IGraphAuthenticationProviderFactory, GraphAuthenticationProviderFactory>();
            services.AddTransient<IReminderService, ReminderService>();
            services.AddTransient<IReminderRepository, ReminderRepository>();
            services.AddTransient<ICalendarService, CalendarService>();
            services.AddTransient<IGraphCalendarProviderFactory, GraphCalendarProviderFactory>();
            services.Configure<ButlerOptions>(Configuration);
            services.AddTransient<IButler, Butler>();
            services.AddTransient<IGoogleCredentialProvider, GoogleCredentialProvider>();
            services.AddTransient<IGoogleCalendarProviderFactory, GoogleCalendarProviderFactory>();
            services.AddTransient<IGoogleCalendarColorProviderFactory, GoogleCalendarColorProviderFactory>();

            services.AddMemoryCache();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration["ServiceIdentityUrl"];
                    options.Audience = "calendar";
                    options.RequireHttpsMetadata = false;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("User", builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.RequireClaim("scope", "calendar.user");
                });
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Service", builder =>
                {
                    builder.RequireClaim("scope", "calendar.service");
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseCors("CorsPolicy");
            app.UseMvc();
        }
    }
}
