using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalendarService.Controllers
{
    public class RemindersController : Controller
    {
        private readonly IReminderService reminderService;

        public RemindersController(IReminderService reminderService)
        {
            this.reminderService = reminderService;
        }

        [HttpPost("api/users/{userId}/reminders")]
        [Authorize("Service")]
        public async Task<IActionResult> RegisterReminder(string userId, [FromBody]ReminderRequest request)
        {
            var reminderRegistration = await reminderService.RegisterAsync(userId, request);
            return Ok(reminderRegistration);
        }

        [HttpPatch("api/users/{userId}/reminders/{id}")]
        [Authorize("Service")]
        public async Task<IActionResult> RenewReminder(string userId, string id)
        {
            var reminderRegistration = await reminderService.RenewAsync(userId, id);
            return Ok(reminderRegistration);
        }
    }
}
