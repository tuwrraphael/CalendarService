using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarService
    {
        /// <returns>null if user has no configurated feeds</returns>
        Task<Models.Event[]> Get(string userId, DateTimeOffset from, DateTimeOffset to);
        Task<Models.Event> GetAsync(string userId, string feedId, string eventId);
        Task InstallNotifications(string userId);
        Task<bool> MaintainNotification(NotificationMaintainanceRequest request);
        Task<string> GetUserIdByNotificationAsync(string notificationId);
    }
}