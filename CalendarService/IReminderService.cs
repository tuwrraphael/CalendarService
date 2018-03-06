using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface IReminderService
    {
        Task<bool> HasActiveReminders(string userId);
    }
}
