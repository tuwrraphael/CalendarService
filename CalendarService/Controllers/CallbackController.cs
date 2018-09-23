using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CalendarService.Controllers
{
    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly ICalendarService calendarService;
        private readonly ILogger<CallbackController> logger;
        private readonly IReminderService reminderService;

        public CallbackController(ICalendarService calendarService,
            ILogger<CallbackController> logger,
            IReminderService reminderService)
        {
            this.calendarService = calendarService;
            this.logger = logger;
            this.reminderService = reminderService;
        }

        [AllowAnonymous]
        [HttpPost("notification-maintainance")]
        public async Task<IActionResult> PatchNotification([FromBody]NotificationMaintainanceRequest req)
        {
            var res = await calendarService.MaintainNotification(req);
            if (res)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [AllowAnonymous]
        [HttpPost("graph")]
        public async Task<IActionResult> GraphCallback(string validationToken)
        {
            if (null != validationToken)
            {
                return new ContentResult
                {
                    ContentType = "text/plain",
                    Content = validationToken,
                    StatusCode = 200
                };
            }
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var text = await reader.ReadToEndAsync();
                var notificaton = JsonConvert.DeserializeObject<GraphNotificationRoot>(text);
                var userId = await calendarService.GetUserIdByNotificationAsync(notificaton.Value[0].ClientState);
                if (null != userId)
                {
                    await reminderService.MaintainRemindersForUserAsync(userId);
                }
                else
                {
                    logger.LogError($"User for notification {notificaton.Value[0].ClientState} not found.");
                }
            }
            return Accepted();
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleCallback(string validationToken)
        {
            if (Request.Headers["X-Goog-Resource-State"] == "sync")
            {
                return Ok();
            }
            var notificationId = Request.Headers["X-Goog-Channel-ID"];
            var userId = await calendarService.GetUserIdByNotificationAsync(notificationId);
            if (null != userId)
            {
                await reminderService.MaintainRemindersForUserAsync(userId);
            }
            else
            {
                logger.LogError($"User for notification {notificationId} not found.");
            }
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("reminder-maintainance")]
        public async Task<IActionResult> ReminderMaintainanceCallback([FromBody]ReminderMaintainanceRequest request)
        {
            await reminderService.MaintainReminderAsync(request.ReminderId);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("reminder-execute")]
        public async Task<IActionResult> ReminderMaintainanceCallback([FromBody]ReminderProcessRequest request)
        {
            var sent = await reminderService.ProcessReminderAsync(request);
            if (sent)
            {
                return Ok();
            }
            return NotFound();
        }
    }
}
