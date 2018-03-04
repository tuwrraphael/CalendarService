using CalendarService.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarProvider
    {
        Task<Event[]> Get(DateTime from, DateTime to);
    }
}
