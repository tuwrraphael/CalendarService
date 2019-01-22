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
        IFeedCollection Feeds { get; }
    }

    public interface IFeedCollection
    {
        IFeed this[string feedId]
        {
            get;
        }
    }

    public interface IFeed
    {
        IFeedEventCollection Events { get; }
    }

    public interface IFeedEventCollection
    {
        Task<Event> Get(string Id);
    }

    public interface IEventCollection
    {
        Task<Event> GetCurrentAsync();
        Task<Event[]> Get(DateTimeOffset? from, DateTimeOffset? to);
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
