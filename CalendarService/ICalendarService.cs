﻿using System;
using System.Threading.Tasks;

namespace CalendarService
{
    public interface ICalendarService
    {
        Task<Event[]> Get(string userId, DateTime from, DateTime to);
    }
}