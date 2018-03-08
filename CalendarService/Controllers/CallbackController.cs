using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalendarService.Controllers
{
    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly ICalendarService calendarService;
        private readonly ILogger<CallbackController> logger;

        public CallbackController(ICalendarService calendarService, ILogger<CallbackController> logger)
        {
            this.calendarService = calendarService;
            this.logger = logger;
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
                logger.LogInformation($"resent token");
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
                logger.LogInformation($"received notification {text}");
                var client = new HttpClient();
                await client.PostAsync("https://digit-app.azurewebsites.net/api/device/12345/log", new StringContent(
                    "{\"code\":0,\"message\": \"Calendar update\"}", Encoding.UTF8, "application/json"));
            }
            return Accepted();
        }
    }
}
