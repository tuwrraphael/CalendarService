using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarProvider
    {
        Task<Models.Event[]> Get(DateTimeOffset from, DateTimeOffset to);
        Task<Models.Event> GetAsync(string feedId, string eventId);
        Task<NotificationInstallation> InstallNotification(string feedId);
        Task<NotificationInstallation> MaintainNotification(NotificationInstallation providerNotifiactionId, string feedId);
    }
}
