using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;
using System.Threading.Tasks;
using System;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOrManager")]
    public class ChatSettingsController : BaseController
    {
        private readonly IOptionsSnapshot<ChatSettings> _chatSettings;

        public ChatSettingsController(
            IOptionsSnapshot<ChatSettings> chatSettings,
            Microsoft.Extensions.Logging.ILogger<ChatSettingsController> logger) : base(logger)
        {
            _chatSettings = chatSettings;
        }

        [HttpGet]
        public IActionResult GetSettings()
        {
            try
            {
                var settings = _chatSettings.Value;
                return Ok(new { success = true, data = settings });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy cấu hình chat");
            }
        }

        [HttpPut("roles")]
        public IActionResult UpdateRoles([FromBody] RoleSettings roles)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                return BadRequest(new {
                    success = false,
                    message = "Cập nhật cấu hình roles cần restart application. Vui lòng cập nhật trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật roles");
            }
        }

        [HttpPut("messages")]
        public IActionResult UpdateMessages([FromBody] MessageSettings messages)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                return BadRequest(new {
                    success = false,
                    message = "Cập nhật cấu hình messages cần restart application. Vui lòng cập nhật trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật messages");
            }
        }

        [HttpPut("pagination")]
        public IActionResult UpdatePagination([FromBody] PaginationSettings pagination)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                return BadRequest(new {
                    success = false,
                    message = "Cập nhật cấu hình pagination cần restart application. Vui lòng cập nhật trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật pagination");
            }
        }

        [HttpPut("signalr")]
        public IActionResult UpdateSignalR([FromBody] SignalRSettings signalR)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                return BadRequest(new {
                    success = false,
                    message = "Cập nhật cấu hình SignalR cần restart application. Vui lòng cập nhật trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật SignalR");
            }
        }

        [HttpPut("assignment")]
        public IActionResult UpdateAssignment([FromBody] AssignmentSettings assignment)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                return BadRequest(new {
                    success = false,
                    message = "Cập nhật cấu hình assignment cần restart application. Vui lòng cập nhật trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật assignment");
            }
        }

        [HttpGet("assignment/enable")]
        public IActionResult EnableAutoAssign()
        {
            try
            {
                return Ok(new {
                    success = false,
                    message = "Vui lòng cập nhật Chat:Assignment:AutoAssignEnabled trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Bật auto assign");
            }
        }

        [HttpGet("assignment/disable")]
        public IActionResult DisableAutoAssign()
        {
            try
            {
                return Ok(new {
                    success = false,
                    message = "Vui lòng cập nhật Chat:Assignment:AutoAssignEnabled trong appsettings.json và restart."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Tắt auto assign");
            }
        }
    }
}

