using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Interfaces;
using System.Threading.Tasks;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet("rate-limiting")]
        public async Task<IActionResult> GetRateLimiting()
        {
            try
            {
                var settings = await _settingsService.GetRateLimitingAsync();
                return Ok(new
                {
                    success = true,
                    message = "Lấy cấu hình rate limiting thành công",
                    data = settings
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        [HttpPut("rate-limiting")]
        public async Task<IActionResult> UpdateRateLimiting([FromBody] UpdateRateLimitingSettingsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState
                });
            }

            try
            {
                await _settingsService.UpdateRateLimitingAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật cấu hình rate limiting thành công",
                    data = request
                });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }
    }
}

