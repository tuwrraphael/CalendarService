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
        private readonly ICalendarService calendarService;
        private readonly IReminderService reminderService;

        public ConfigurationController(ICalendarConfigurationService calendarConfigurationService,
            ICalendarService calendarService,
            IReminderService reminderService)
        {
            this.calendarConfigurationService = calendarConfigurationService;
            this.calendarService = calendarService;
            this.reminderService = reminderService;
        }

        [HttpGet("list")]
        [Authorize("User")]
        public async Task<IActionResult> List()
        {
            return Ok(await calendarConfigurationService.GetConfigurations(User.GetId()));
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
                    return Ok(new CalendarLinkResponse()
                    {
                        RedirectUri = await calendarConfigurationService.GetMicrosoftLinkUrl(userId, request.RedirectUri)
                    });
                case CalendarType.Google:
                    return Ok(new CalendarLinkResponse()
                    {
                        RedirectUri = await calendarConfigurationService.GetGoogleLinkUrl(userId, request.RedirectUri)
                    });
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
            try
            {
                var res = await calendarConfigurationService.LinkGoogle(state, code);
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

        [Authorize("User")]
        [HttpPut("{id}/feeds")]
        public async Task<IActionResult> SetFeeds(string id, [FromBody]string[] feedIds)
        {
            var userId = User.GetId();
            var changed = await calendarConfigurationService.SetFeeds(userId, id, feedIds);
            if (await reminderService.HasActiveAsync(userId))
            {
                await calendarService.InstallNotifications(User.GetId());
                await reminderService.MaintainRemindersForUserAsync(User.GetId());
            }
            if (changed)
            {
                return Ok();
            }
            return NotFound();
        }
    }
}
