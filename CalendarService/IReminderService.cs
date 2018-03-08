using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarService.Controllers;

namespace CalendarService
{
    public interface IReminderService
    {
        Task<ReminderRegistration> RegisterAsync(string userId, ReminderRequest request);
        Task<ReminderRegistration> RenewAsync(string userId, string id);
        Task<bool> HasActiveAsync(string userId);
    }
}
