using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ButlerClient;
using CalendarService.Models;
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

        private const uint ExpirationMinutes = 2 * 60 * 24;

        private const int ReminderDeletionGracePeriod = 5;
        private const int EventDiscoveryOverlap = 1;

        private static readonly TimeSpan MinReminderFuture = new TimeSpan(3 * 24, 0, 0);

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

        public async Task PostReminder(StoredReminder reminder, ReminderDelivery delivery)
        {
            var client = new HttpClient();
            var res = await client.PostAsync(reminder.NotificationUri,
                new StringContent(JsonConvert.SerializeObject(delivery), Encoding.UTF8, "application/json"));
            if (!res.IsSuccessStatusCode)
            {
                throw new ReminderDeliveryException();
            }
        }

        public async Task<bool> ProcessReminderAsync(ReminderProcessRequest request)
        {
            var reminder = await reminderRepository.GetAsync(request.ReminderId);
            if (null == reminder)
            {
                return false; //reminder expired
            }
            var instance = reminder.Instances.Where(v => v.Hash == request.Hash &&
                v.Id == request.InstanceId).Single();
            if (null == instance)
            {
                return false; //this happens when the Hash was updated and butlerservice does not allow to delete hooks
            }
            var ev = await calendarService.GetAsync(reminder.UserId, instance.FeedId, instance.EventId);
            await PostReminder(reminder, new ReminderDelivery
            {
                ReminderId = reminder.Id,
                Event = ev,
                ClientState = reminder.ClientState,
                FeedId = ev.FeedId,
                EventId = ev.Id,
                Removed = false
            });
            await reminderRepository.RemindRemovalUntilAsync(request.InstanceId, ev.End);
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
                    Hash = instance.Hash
                },
                Url = options.ProcessReminderUri,
                When = e.Start.AddMinutes(-reminder.Minutes).UtcDateTime
            });
        }

        private async Task UpdateReminderAsync(StoredReminder reminder)
        {
            //add a small threshold to prevent off by one errors
            var futureTime = Math.Max(MinReminderFuture.TotalMinutes, reminder.Minutes) + EventDiscoveryOverlap;
            var evts = (await calendarService.Get(reminder.UserId, DateTimeOffset.Now,
                DateTimeOffset.Now.AddMinutes(futureTime))) ?? new Event[0];
            var events = (evts).Where(v => v.Start >= DateTimeOffset.Now);
            var existingInstances = new List<ReminderInstance>(reminder.Instances);
            foreach (var e in events)
            {
                var instance = reminder.Instances.Where(v => v.EventId == e.Id && v.FeedId == e.FeedId).FirstOrDefault();
                var shouldFire = e.Start.AddMinutes(-reminder.Minutes) <= DateTimeOffset.Now;
                var eventHash = e.GenerateHash();
                if (null != instance)
                {
                    existingInstances.Remove(instance);
                    if (instance.Hash != eventHash)
                    {
                        instance = await reminderRepository.UpdateInstanceAsync(instance.Id, eventHash);
                    }
                }
                else
                {
                    instance = new ReminderInstance()
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventId = e.Id,
                        FeedId = e.FeedId,
                        Hash = eventHash
                    };
                    await reminderRepository.AddInstanceAsync(reminder.Id, instance);
                }
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
                        Hash = eventHash
                    });
                }
            }
            foreach (var instance in existingInstances)
            {
                if (instance.RemindRemovalUntil.HasValue && instance.RemindRemovalUntil > DateTimeOffset.Now.UtcDateTime)
                {
                    await PostReminder(reminder, new ReminderDelivery
                    {
                        ReminderId = reminder.Id,
                        ClientState = reminder.ClientState,
                        Event = null,
                        EventId = instance.EventId,
                        FeedId = instance.FeedId,
                        Removed = true
                    });
                }
                await reminderRepository.RemoveInstanceAsync(instance.Id);
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
