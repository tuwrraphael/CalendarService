using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface IReminderRepository
    {
        Task AddAsync(string userId, ReminderRequest request, DateTime expires);
        Task RenewAsync(string userId, ReminderRegistration registration);
        Task<bool> HasActiveReminders(string userId);
        Task<StoredReminder> GetAsync(string reminderId);
    }
}