using CalendarService.Models;
using System.Threading.Tasks;

namespace CalendarService.Client
{
    public interface ICalendarServiceClient
    {
        Task<Event> GetCurrentEventAsync(string userId);
        Task<ReminderRegistration> RegisterReminderAsync(string userId, ReminderRequest request);
        Task<ReminderRegistration> RenewReminderAsync(string userId, string id);
        Task<bool> ReminderAliveAsync(string userId, string reminderId);
    }
}
