using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalendarService.Controllers
{
    [Route("api/[controller]")]
    public class ConfigurationController : Controller
    {
        private readonly ICalendarConfigurationService calendarConfigurationService;

        public ConfigurationController(ICalendarConfigurationService calendarConfigurationService)
        {
            this.calendarConfigurationService = calendarConfigurationService;
        }

        [Authorize("User")]
        [HttpPost("link")]
        public async Task<IActionResult> Link([FromBody]CalendarLinkRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var userId = User.GetId();
            switch (request.CalendarType)
            {
                case CalendarType.Microsoft:
                    var url = await calendarConfigurationService.GetMicrosoftLinkUrl(userId, request.RedirectUri);
                    return Ok(new CalendarLinkResponse()
                    {
                        RedirectUri = url
                    });
                case CalendarType.Google:
                default:
                    return BadRequest();
            }
        }

        [HttpGet("ms-connect")]
        public async Task<IActionResult> MicrosoftConnect([Required]string code, [Required]string state)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            try
            {
                var res = await calendarConfigurationService.LinkMicrosoft(state, code);
                if (null != res.RedirectUri)
                {
                    return Redirect(res.RedirectUri);
                }
                return Ok();
            }
            catch (AuthorizationStateNotFoundException)
            {
                return BadRequest("state invalid");
            }
        }

        [HttpGet("google-connect")]
        public async Task<IActionResult> GoogleConnect([Required]string code, [Required]string state)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            await calendarConfigurationService.LinkGoogle(state, code);
            return Ok();
        }

        [Authorize("User")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.GetId();
            var deleted = await calendarConfigurationService.RemoveConfig(userId, id);
            if (deleted)
            {
                return Ok();
            }
            return NotFound();
        }
    }
}
