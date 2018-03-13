using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalendarService.Controllers
{
    [Route("api/[controller]")]
    public class CalendarController : Controller
    {
        private readonly ICalendarService calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            this.calendarService = calendarService;
        }

        private async Task<IActionResult> GetCalendarForUser(string userId, DateTime? from, DateTime? to)
        {
            if (!from.HasValue)
            {
                from = DateTime.Now;
            }
            if (!to.HasValue)
            {
                to = DateTime.Now.AddHours(24);
            }
            var result = await calendarService.Get(userId, from.Value, to.Value);
            if (null == result)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize("User")]
        public async Task<IActionResult> GetOwn(DateTime? from, DateTime? to)
        {
            return await GetCalendarForUser(User.GetId(), from, to);
        }

        [HttpGet("{userId}")]
        [Authorize("Service")]
        public async Task<IActionResult> GetForUser(string userId, DateTime? from, DateTime? to)
        {
            return await GetCalendarForUser(userId, from, to);
        }
    }
}
