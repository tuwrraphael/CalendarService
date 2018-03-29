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

        private const uint ExpirationMinutes = 60 * 4;

        private const int ReminderDeletionGracePeriod = 5;
        private const int EventDiscoveryOverlap = 1;

        private static readonly TimeSpan MinReminderFuture = new TimeSpan(24, 0, 0);

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
            if (null == reminder)
            {
                return false; //reminder expired
            }
            var instance = reminder.Instances.Where(v => v.Revision == request.Revision &&
                v.Id == request.InstanceId).Single();
            if (null == instance)
            {
                return false; //this happens when the revision was updated and butlerservice does not allow to delete hooks
            }
            var ev = await calendarService.GetAsync(reminder.UserId, instance.FeedId, instance.EventId);
            var client = new HttpClient();
            var res = await client.PostAsync(reminder.NotificationUri, new StringContent(JsonConvert.SerializeObject(new ReminderDelivery
            {
                ReminderId = reminder.Id,
                Event = ev,
                ClientState = reminder.ClientState
            }), Encoding.UTF8, "application/json"));
            if (!res.IsSuccessStatusCode)
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
            await reminderRepository.AddAsync(userId, request, registration);
            await calendarService.InstallNotifications(userId);
            await MaintainReminderAsync(registration.Id);
            return registration;
        }

        private async Task<string> InstallButlerForInstance(StoredReminder reminder, ReminderInstance instance, Event e)
        {
            return await butler.InstallAsync(new WebhookRequest()
            {
                Data = new ReminderProcessRequest()
                {
                    InstanceId = instance.Id,
                    ReminderId = reminder.Id,
                    Revision = instance.Revision
                },
                Url = options.ProcessReminderUri,
                When = e.Start.AddMinutes(-reminder.Minutes)
            });
        }

        private async Task UpdateReminderAsync(StoredReminder reminder)
        {
            //add a small threshold to prevent off by one errors
            var futureTime = Math.Max(MinReminderFuture.TotalMinutes, reminder.Minutes) + EventDiscoveryOverlap;
            var events = (await calendarService.Get(reminder.UserId, DateTime.Now,
                DateTime.Now.AddMinutes(futureTime))).Where(v => v.Start >= DateTime.Now);
            if (null != events)
            {
                foreach (var e in events)
                {
                    var instance = reminder.Instances.Where(v => v.EventId == e.Id && v.FeedId == e.FeedId).SingleOrDefault();
                    var shouldFire = e.Start.AddMinutes(-reminder.Minutes) <= DateTime.Now;
                    if (null != instance)
                    {
                        if (instance.Start != e.Start)
                        {
                            instance = await reminderRepository.UpdateInstanceAsync(instance.Id, e.Start, instance.Revision + 1);
                            if (!shouldFire)
                            {
                                await InstallButlerForInstance(reminder, instance, e);
                            }
                            else
                            {
                                await ProcessReminderAsync(new ReminderProcessRequest()
                                {
                                    InstanceId = instance.Id,
                                    ReminderId = reminder.Id,
                                    Revision = instance.Revision
                                });
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
                        await reminderRepository.AddInstanceAsync(reminder.Id, instance);
                        if (!shouldFire)
                        {
                            await InstallButlerForInstance(reminder, instance, e);
                        }
                        else
                        {
                            await ProcessReminderAsync(new ReminderProcessRequest()
                            {
                                InstanceId = instance.Id,
                                ReminderId = reminder.Id,
                                Revision = instance.Revision
                            });
                        }
                    }
                }
            }
        }

        public async Task MaintainReminderAsync(string reminderId)
        {
            var reminder = await reminderRepository.GetAsync(reminderId);
            var now = DateTime.Now;
            if (now >= reminder.Expires.AddMinutes(ReminderDeletionGracePeriod))
            {
                await reminderRepository.DeleteAsync(reminderId);
            }
            else
            {
                await UpdateReminderAsync(reminder);
                var aliveTime = reminder.Expires.AddMinutes(ReminderDeletionGracePeriod) - now;
                var when = now.AddMinutes(Math.Min(aliveTime.TotalMinutes, Math.Max(MinReminderFuture.TotalMinutes, reminder.Minutes) - EventDiscoveryOverlap));
                await butler.InstallAsync(new WebhookRequest()
                {
                    Url = options.MaintainRemindersUri,
                    Data = new ReminderMaintainanceRequest()
                    {
                        ReminderId = reminderId
                    },
                    When = when
                });
            }
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

        public async Task MaintainRemindersForUserAsync(string userId)
        {
            var reminders = await reminderRepository.GetActiveForUserAsync(userId);
            foreach (var reminder in reminders)
            {
                await UpdateReminderAsync(reminder);
            }
        }

        public async Task<ReminderRegistration> GetAsync(string userId, string id)
        {
            var reminder = await reminderRepository.GetAsync(id);
            if (null == reminder || DateTime.Now >= reminder.Expires)
            {
                return null;
            }
            return new ReminderRegistration()
            {
                Id = reminder.Id,
                Expires = reminder.Expires
            };
        }
    }
}
