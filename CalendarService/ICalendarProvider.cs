using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarProvider
    {
        Task<Models.Event[]> Get(DateTime from, DateTime to);
        Task<Models.Event> GetAsync(string feedId, string eventId);
        Task<NotificationInstallation> InstallNotification(string feedId);
        Task<NotificationInstallation> MaintainNotification(NotificationInstallation providerNotifiactionId);
    }
}
