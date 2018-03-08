using System;
using System.Linq;
using System.Threading.Tasks;
using ButlerClient;
using Microsoft.Extensions.Options;

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

        public async Task ProcessReminderAsync(ReminderProcessRequest request)
        {
            //search for reminder instance and revision
            //call the webhook
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
            await MaintainReminders(userId);
            return registration;
        }

        public async Task MaintainReminders(string userId)
        {
            var reminders = await reminderRepository.GetAsync(userId);
            if (null == reminders || 0 == reminders.Length)
                return;
            var longest = reminders.Max(v => v.Minutes) + 10; //add a small threshold to prevent off by one errors
            var futureTime = Math.Max(MinReminderFuture.TotalMinutes, longest);
            var events = await calendarService.Get(userId, DateTime.Now,
                DateTime.Now.AddMinutes(futureTime));
            foreach (var e in events)
            {
                foreach (var r in reminders)
                {
                    //search for existing reminder instance - store reminder id with it
                    //if found and time changed register butler for it; update revision
                    //if not found create one & register butler for it, revision = 0
                    //if e should fire, call ProcessReminder instead of registering butler
                }
            }
            await butler.InstallAsync(new WebhookRequest()
            {
                Url = options.MaintainRemindersUri,
                Data = new ReminderMaintainaceRequest()
                {
                    UserId = userId
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
