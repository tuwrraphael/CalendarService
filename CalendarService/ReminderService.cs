using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ButlerClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CalendarService
{
    public class ReminderService : IReminderService
    {
        private readonly IReminderRepository reminderRepository;
        private readonly ICalendarService calendarService;
        private readonly IButler butler;
        private readonly CalendarConfigurationOptions options;

        private const uint ExpirationMinutes = 2;

        private static readonly TimeSpan MinReminderFuture = new TimeSpan(1, 0, 0);

        public ReminderService(IReminderRepository reminderRepository,
            ICalendarService calendarService,
            IButler butler,
            IOptions<CalendarConfigurationOptions> optionsAccessor)
        {
            this.reminderRepository = reminderRepository;
            this.calendarService = calendarService;
            this.butler = butler;
            options = optionsAccessor.Value;
        }

        public async Task<bool> ProcessReminderAsync(ReminderProcessRequest request)
        {
            var reminder = await reminderRepository.GetAsync(request.ReminderId);
            var instance = reminder.Instances.Where(v => v.Revision == request.Revision &&
                v.Id == request.InstanceId).Single();
            if (null == instance)
            {
                return false; //this happens when the revision was updated and butlerservice does not allow to delete hooks
            }
            var client = new HttpClient();
            var res = await client.PostAsync(reminder.NotificationUri, new StringContent(JsonConvert.SerializeObject(new EventReminder
            {
                ReminderId = reminder.Id,
                Event = new Event()
                {
                    FeedId = instance.FeedId,
                    Start = instance.Start,
                    Id = instance.Id
                },
                ClientState = reminder.ClientState
            }), Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode)
            {
                throw new ReminderDeliveryException();
            }
            return true;
        }

        public async Task<ReminderRegistration> RegisterAsync(string userId, ReminderRequest request)
        {
            var registration = new ReminderRegistration()
            {
                Expires = DateTime.Now.AddMinutes(ExpirationMinutes),
                Id = Guid.NewGuid().ToString()
            };
            await reminderRepository.AddAsync(userId, request, registration.Expires);
            await calendarService.InstallNotifications(userId);
            await MaintainReminder(registration.Id);
            return registration;
        }

        private async Task<string> InstallButlerForInstance(string reminderId, ReminderInstance instance, Event e)
        {
            return await butler.InstallAsync(new WebhookRequest()
            {
                Data = new ReminderProcessRequest()
                {
                    InstanceId = instance.Id,
                    ReminderId = reminderId,
                    Revision = instance.Revision
                },
                Url = options.ProcessReminderUri,
                When = e.Start
            });
        }

        public async Task MaintainReminder(string reminderId)
        {
            var reminder = await reminderRepository.GetAsync(reminderId);
            //add a small threshold to prevent off by one errors
            var futureTime = Math.Max(MinReminderFuture.TotalMinutes, reminder.Minutes) + 10;
            var events = await calendarService.Get(reminder.UserId, DateTime.Now,
                DateTime.Now.AddMinutes(futureTime));
            foreach (var e in events)
            {
                var instance = reminder.Instances.Where(v => v.EventId == e.Id && v.FeedId == e.FeedId).SingleOrDefault();
                var shouldFire = e.Start <= DateTime.Now;
                if (null != instance)
                {
                    if (instance.Start != e.Start)
                    {
                        instance.Start = e.Start;
                        instance.Revision++;
                        await reminderRepository.UpdateInstanceAsync(reminderId, instance);
                        if (!shouldFire)
                        {
                            await InstallButlerForInstance(reminderId, instance, e);
                        }
                    }
                }
                else
                {
                    instance = new ReminderInstance()
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventId = e.Id,
                        FeedId = e.FeedId,
                        Revision = 0,
                        Start = e.Start
                    };
                    await reminderRepository.AddInstanceAsync(reminderId, instance);
                    if (!shouldFire)
                    {
                        await InstallButlerForInstance(reminderId, instance, e);
                    }
                }
                if (shouldFire)
                {
                    await ProcessReminderAsync(new ReminderProcessRequest()
                    {
                        InstanceId = instance.Id,
                        ReminderId = reminderId,
                        Revision = instance.Revision
                    });
                }
            }
            await butler.InstallAsync(new WebhookRequest()
            {
                Url = options.MaintainRemindersUri,
                Data = new ReminderMaintainaceRequest()
                {
                    ReminderId = reminderId
                }
            });
        }

        public async Task<ReminderRegistration> RenewAsync(string userId, string id)
        {
            var registration = new ReminderRegistration()
            {
                Expires = DateTime.Now.AddMinutes(ExpirationMinutes),
                Id = id
            };
            await reminderRepository.RenewAsync(userId, registration);
            return registration;
        }

        public async Task<bool> HasActiveAsync(string userId)
        {
            return await reminderRepository.HasActiveReminders(userId);
        }
    }
}
