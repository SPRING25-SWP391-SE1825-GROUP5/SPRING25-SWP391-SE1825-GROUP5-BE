using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Api.Constants;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EVServiceCenter.Api.Controllers
{
    [Route("api/configuration")]
    public class ConfigurationController : BaseController
    {
        private readonly ILoginLockoutService _loginLockoutService;
        private readonly EVServiceCenter.Application.Interfaces.ISettingsService _settingsService;

        public ConfigurationController(ILoginLockoutService loginLockoutService, EVServiceCenter.Application.Interfaces.ISettingsService settingsService, ILogger<ConfigurationController> logger) : base(logger)
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
                return Ok(new { success = true, message = "Lấy cấu hình Login Lockout thành công", data = config });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetConfig");
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
                var validationError = ValidateModelState();
                if (validationError != null) return validationError;

                await _loginLockoutService.UpdateConfigAsync(request);
                return Ok(new { success = true, message = "Cập nhật cấu hình Login Lockout thành công" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "UpdateConfig");
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

        // ============================================================================
        // PUBLIC CONFIG ENDPOINTS FOR FRONTEND
        // ============================================================================

        /// <summary>
        /// Lấy feature flags cho frontend (public endpoint)
        /// </summary>
        /// <returns>Danh sách feature flags</returns>
        [HttpGet("features")]
        [AllowAnonymous]
        public IActionResult GetFeatures()
        {
            try
            {
                var features = new
                {
                    enableMaintenanceReminder = true,
                    enableSoftWarning = true,
                    enableGuestBooking = true,
                    enableRealTimeBooking = true,
                    enablePromotions = true,
                    enableFeedback = true,
                    enableNotifications = true,
                    enableFileUpload = true,
                    enableMultiplePaymentMethods = true,
                    enableBookingHistory = true,
                    enableOrderHistory = true,
                    enableVehicleManagement = true,
                    enableTechnicianAssignment = true,
                    enableInventoryManagement = true,
                    enableReports = true
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy feature flags thành công",
                    data = features
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi lấy feature flags",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy business rules cho frontend (public endpoint)
        /// </summary>
        /// <returns>Danh sách business rules</returns>
        [HttpGet("rules")]
        [AllowAnonymous]
        public IActionResult GetRules()
        {
            try
            {
                var rules = new
                {
                    pagination = new
                    {
                        defaultPageSize = ApiConstants.Pagination.DefaultPageSize,
                        maxPageSize = ApiConstants.Pagination.MaxPageSize,
                        minPageSize = ApiConstants.Pagination.MinPageSize
                    },
                    fileUpload = new
                    {
                        maxSizeBytes = ApiConstants.FileUpload.MaxSizeBytes,
                        allowedExtensions = ApiConstants.FileUpload.AllowedExtensions,
                        maxFilesPerUpload = ApiConstants.FileUpload.MaxFilesPerUpload
                    },
                    booking = new
                    {
                        maxAdvanceBookingDays = ApiConstants.Booking.MaxAdvanceBookingDays,
                        minAdvanceBookingHours = ApiConstants.Booking.MinAdvanceBookingHours,
                        maxBookingDurationHours = ApiConstants.Booking.MaxBookingDurationHours,
                        allowCancellationHours = ApiConstants.Booking.AllowCancellationHours
                    },
                    validation = new
                    {
                        minPasswordLength = ApiConstants.Validation.MinPasswordLength,
                        maxPasswordLength = ApiConstants.Validation.MaxPasswordLength,
                        phoneNumberPattern = ApiConstants.Validation.PhoneNumberPattern,
                        emailPattern = ApiConstants.Validation.EmailPattern,
                        licensePlatePattern = ApiConstants.Validation.LicensePlatePattern
                    },
                    limits = new
                    {
                        maxVehiclesPerCustomer = ApiConstants.Limits.MaxVehiclesPerCustomer,
                        maxBookingsPerDay = ApiConstants.Limits.MaxBookingsPerDay,
                        maxPromotionsPerCustomer = ApiConstants.Limits.MaxPromotionsPerCustomer,
                        maxFeedbackLength = ApiConstants.Validation.MaxFeedbackLength
                    },
                    timeouts = new
                    {
                        sessionTimeoutMinutes = ApiConstants.Timeouts.SessionTimeoutMinutes,
                        guestSessionTimeoutMinutes = ApiConstants.Timeouts.GuestSessionTimeoutMinutes,
                        otpExpiryMinutes = ApiConstants.Timeouts.OtpExpiryMinutes,
                        lockoutDurationMinutes = ApiConstants.Timeouts.LockoutDurationMinutes
                    }
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy business rules thành công",
                    data = rules
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi lấy business rules",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy public settings cho frontend (public endpoint)
        /// </summary>
        /// <returns>Public settings</returns>
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult GetPublicSettings()
        {
            try
            {
                var settings = new
                {
                    app = new
                    {
                        name = "EV Service Center",
                        version = "1.0.0",
                        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                    },
                    api = new
                    {
                        baseUrl = $"{Request.Scheme}://{Request.Host}",
                        version = "v1",
                        supportedVersions = new[] { "v1" }
                    },
                    endpoints = new
                    {
                        auth = ApiConstants.Endpoints.Auth,
                        booking = ApiConstants.Endpoints.Booking,
                        services = ApiConstants.Endpoints.Services,
                        vehicles = ApiConstants.Endpoints.Vehicles,
                        promotions = ApiConstants.Endpoints.Promotions,
                        feedback = ApiConstants.Endpoints.Feedback,
                        swagger = ApiConstants.Endpoints.Swagger
                    },
                    support = new
                    {
                        email = ApiConstants.Support.Email,
                        phone = ApiConstants.Support.Phone,
                        workingHours = ApiConstants.Support.WorkingHours
                    }
                };

                return Ok(new
                {
                    success = true,
                    message = "Lấy public settings thành công",
                    data = settings
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi khi lấy public settings",
                    error = ex.Message
                });
            }
        }
    }
}
