using System.Threading.Tasks;

namespace CalendarService
{
    public interface IReminderService
    {
        Task<ReminderRegistration> RegisterAsync(string userId, ReminderRequest request);
        Task<ReminderRegistration> RenewAsync(string userId, string id);
        Task<bool> HasActiveAsync(string userId);
        Task MaintainReminderAsync(string reminderId);
        Task MaintainRemindersForUserAsync(string userId);
        Task<bool> ProcessReminderAsync(ReminderProcessRequest request);
    }
}

