using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarProvider
    {
        Task<Event[]> Get(DateTime from, DateTime to);
        Task<Event> GetAsync(string feedId, string eventId);
        Task<NotificationInstallation> InstallNotification(string feedId);
        Task<NotificationInstallation> MaintainNotification(NotificationInstallation providerNotifiactionId);
    }
}
