using CalendarService.Models;
using System;
using System.Threading.Tasks;

namespace CalendarService.Client
{
    public interface ICalendarServiceClient
    {
        IUserCollection Users { get; }
    }
    public interface IUserCollection
    {
        IUser this[string userId]
        {
            get;
        }
    }
    public interface IUser
    {
        IReminderCollection Reminders { get; }
        IEventCollection Events { get; }
    }

    public interface IEventCollection
    {
        Task<Event> GetCurrentAsync();
        Task<Event[]> Get(DateTime? from, DateTime? to);
    }

    public interface IReminderCollection
    {
        IReminder this[string reminderId]
        {
            get;
        }
        Task<ReminderRegistration> RegisterAsync(ReminderRequest request);
    }

    public interface IReminder
    {
        Task<ReminderRegistration> RenewAsync();
        Task<bool> IsAliveAsync();
    }
}
