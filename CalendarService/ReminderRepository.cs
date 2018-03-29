using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarService
{
    public class ReminderRepository : IReminderRepository
    {
        private readonly CalendarServiceContext context;

        public ReminderRepository(CalendarServiceContext calendarServiceContext)
        {
            context = calendarServiceContext;
        }

        public async Task AddAsync(string userId, ReminderRequest request, ReminderRegistration registration)
        {
            var user = await context.Users.Where(v => v.Id == userId).SingleOrDefaultAsync();
            var storedReminder = new StoredReminder()
            {
                ClientState = request.ClientState,
                Expires = registration.Expires,
                Id = registration.Id,
                Minutes = request.Minutes,
                NotificationUri = request.NotificationUri
            };
            if (null == user)
            {
                user = new User()
                {
                    Id = userId,
                    Reminders = new List<StoredReminder>() {
                        storedReminder
                    }
                };
                await context.Users.AddAsync(user);
            }
            else
            {
                if (null == user.Reminders)
                {
                    user.Reminders = new List<StoredReminder>();
                }
                user.Reminders.Add(storedReminder);
            }
            await context.SaveChangesAsync();
        }

        public async Task AddInstanceAsync(string reminderId, ReminderInstance instance)
        {
            var reminder = await context.Reminders.Include(v => v.Instances).Where(v => v.Id == reminderId).SingleAsync();
            if (reminder.Instances == null)
            {
                reminder.Instances = new List<ReminderInstance>();
            }
            reminder.Instances.Add(instance);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string reminderId)
        {
            var reminder = await context.Reminders.Where(v => v.Id == reminderId).SingleAsync();
            context.Reminders.Remove(reminder);
            await context.SaveChangesAsync();
        }

        public async Task<StoredReminder> GetAsync(string reminderId) =>
            await context.Reminders.Include(v => v.Instances).Where(v => v.Id == reminderId).SingleOrDefaultAsync();

        public async Task<StoredReminder[]> GetActiveForUserAsync(string userId)
        {
            return await context.Reminders.Include(v => v.Instances).Where(v => v.UserId == userId && v.Expires > DateTime.Now).ToArrayAsync();
        }

        public async Task<bool> HasActiveReminders(string userId) =>
            await context.Reminders.Where(v => v.UserId == userId && v.Expires >= DateTime.Now).AnyAsync();


        public async Task RenewAsync(string userId, ReminderRegistration registration)
        {
            var reminder = await context.Reminders.Where(v => v.UserId == userId && v.Id == registration.Id).SingleAsync();
            reminder.Expires = registration.Expires;
            await context.SaveChangesAsync();
        }

        public async Task<ReminderInstance> UpdateInstanceAsync(string id, DateTime start, int revision)
        {
            var inst = await context.ReminderInstances.Where(v => v.Id == id).SingleAsync();
            inst.Start = start;
            inst.Revision = revision;
            await context.SaveChangesAsync();
            return inst;
        }
    }
}