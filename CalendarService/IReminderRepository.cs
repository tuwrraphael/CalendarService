using CalendarService.Models;
using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface IReminderRepository
    {
        Task AddAsync(string userId, ReminderRequest request, ReminderRegistration registration);
        Task RenewAsync(string userId, ReminderRegistration registration);
        Task<bool> HasActiveReminders(string userId);
        Task<StoredReminder> GetAsync(string reminderId);
        Task AddInstanceAsync(string reminderId, ReminderInstance instance);
        Task DeleteAsync(string reminderId);
        Task<StoredReminder[]> GetActiveForUserAsync(string userId);
        Task<ReminderInstance> UpdateInstanceAsync(string id, string hash);
        Task RemindRemovalUntilAsync(string instanceId, DateTimeOffset end);
        Task RemoveInstanceAsync(string instanceId);
    }
}