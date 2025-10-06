using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/configuration")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ILoginLockoutService _loginLockoutService;
        private readonly EVServiceCenter.Application.Interfaces.ISettingsService _settingsService;

        public ConfigurationController(ILoginLockoutService loginLockoutService, EVServiceCenter.Application.Interfaces.ISettingsService settingsService)
        {
            _loginLockoutService = loginLockoutService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Lấy cấu hình Login Lockout hiện tại
        /// </summary>
        /// <returns>Cấu hình Login Lockout</returns>
        [HttpGet("login-lockout/config")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetConfig()
        {
            try
            {
                var config = await _loginLockoutService.GetConfigAsync();
                return Ok(new
                {
                    success = true,
                    message = "Lấy cấu hình Login Lockout thành công",
                    data = config
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi lấy cấu hình Login Lockout",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật cấu hình Login Lockout
        /// </summary>
        /// <param name="request">Thông tin cấu hình mới</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("login-lockout/config")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateConfig([FromBody] LoginLockoutConfigRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                }

                await _loginLockoutService.UpdateConfigAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật cấu hình Login Lockout thành công"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi cập nhật cấu hình Login Lockout",
                    error = ex.Message
                });
            }
        }

        // ------------------- Admin Config: BookingRealtime -------------------
        [HttpGet("booking-realtime")] 
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetBookingRealtime()
        {
            var config = await _settingsService.GetBookingRealtimeAsync();
            return Ok(new { success = true, data = config });
        }

        [HttpPut("booking-realtime")] 
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateBookingRealtime([FromBody] EVServiceCenter.Application.Interfaces.UpdateBookingRealtimeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            await _settingsService.UpdateBookingRealtimeAsync(request);
            return Ok(new { success = true, message = "Cập nhật BookingRealtime thành công" });
        }

        // ------------------- Admin Config: PayOS -------------------
        [HttpGet("payos")] 
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetPayOs()
        {
            var config = await _settingsService.GetPayOsAsync();
            return Ok(new { success = true, data = config });
        }

        [HttpPut("payos")] 
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdatePayOs([FromBody] EVServiceCenter.Application.Interfaces.UpdatePayOsSettingsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            await _settingsService.UpdatePayOsAsync(request);
            return Ok(new { success = true, message = "Cập nhật PayOS thành công" });
        }

        // ------------------- Admin Config: GuestSession -------------------
        [HttpGet("guest-session")] 
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetGuestSession()
        {
            var config = await _settingsService.GetGuestSessionAsync();
            return Ok(new { success = true, data = config });
        }

        [HttpPut("guest-session")] 
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateGuestSession([FromBody] EVServiceCenter.Application.Interfaces.UpdateGuestSessionSettingsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            await _settingsService.UpdateGuestSessionAsync(request);
            return Ok(new { success = true, message = "Cập nhật GuestSession thành công" });
        }

        // ------------------- Admin Config: MaintenanceReminder -------------------
        [HttpGet("maintenance-reminder")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetMaintenanceReminder()
        {
            var config = await _settingsService.GetMaintenanceReminderAsync();
            return Ok(new { success = true, data = config });
        }

        [HttpPut("maintenance-reminder")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateMaintenanceReminder([FromBody] EVServiceCenter.Application.Interfaces.UpdateMaintenanceReminderSettingsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            await _settingsService.UpdateMaintenanceReminderAsync(request);
            return Ok(new { success = true, message = "Cập nhật MaintenanceReminder thành công" });
        }

        /// <summary>
        /// Kiểm tra trạng thái lockout của một email
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <returns>Thông tin lockout</returns>
        [HttpGet("login-lockout/status/{email}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetLockoutStatus(string email)
        {
            try
            {
                var isLocked = await _loginLockoutService.IsAccountLockedAsync(email);
                var remainingAttempts = await _loginLockoutService.GetRemainingAttemptsAsync(email);
                var lockoutExpiry = await _loginLockoutService.GetLockoutExpiryAsync(email);

                return Ok(new
                {
                    success = true,
                    message = "Lấy trạng thái lockout thành công",
                    data = new
                    {
                        email = email,
                        isLocked = isLocked,
                        remainingAttempts = remainingAttempts,
                        lockoutExpiry = lockoutExpiry
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi lấy trạng thái lockout",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Xóa lockout của một email (chỉ dành cho Admin)
        /// </summary>
        /// <param name="email">Email cần xóa lockout</param>
        /// <returns>Kết quả xóa lockout</returns>
        [HttpDelete("login-lockout/unlock/{email}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UnlockAccount(string email)
        {
            try
            {
                await _loginLockoutService.ClearFailedAttemptsAsync(email);
                return Ok(new
                {
                    success = true,
                    message = $"Đã mở khóa tài khoản {email} thành công"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi mở khóa tài khoản",
                    error = ex.Message
                });
            }
        }
    }
}
