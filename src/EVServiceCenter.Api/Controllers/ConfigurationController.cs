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
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;

namespace EVServiceCenter.Api.Controllers
{
    [Route("api/configuration")]
    public class ConfigurationController : BaseController
    {
        private readonly ILoginLockoutService _loginLockoutService;
        private readonly EVServiceCenter.Application.Interfaces.ISettingsService _settingsService;
        private readonly IOptionsSnapshot<FeatureFlagsOptions> _featureFlagsOptions;

        public ConfigurationController(ILoginLockoutService loginLockoutService, EVServiceCenter.Application.Interfaces.ISettingsService settingsService, IOptionsSnapshot<FeatureFlagsOptions> featureFlagsOptions, ILogger<ConfigurationController> logger) : base(logger)
        {
            _loginLockoutService = loginLockoutService;
            _settingsService = settingsService;
            _featureFlagsOptions = featureFlagsOptions;
        }

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

        [HttpGet("features")]
        [AllowAnonymous]
        public IActionResult GetFeatures()
        {
            try
            {
                var flags = _featureFlagsOptions.Value ?? new FeatureFlagsOptions();

                var features = new
                {
                    enableMaintenanceReminder = flags.EnableMaintenanceReminder,
                    enableSoftWarning = flags.EnableSoftWarning,
                    enableGuestBooking = flags.EnableGuestBooking,
                    enableRealTimeBooking = flags.EnableRealTimeBooking,
                    enablePromotions = flags.EnablePromotions,
                    enableFeedback = flags.EnableFeedback,
                    enableNotifications = flags.EnableNotifications,
                    enableFileUpload = flags.EnableFileUpload,
                    enableMultiplePaymentMethods = flags.EnableMultiplePaymentMethods,
                    enableBookingHistory = flags.EnableBookingHistory,
                    enableOrderHistory = flags.EnableOrderHistory,
                    enableVehicleManagement = flags.EnableVehicleManagement,
                    enableTechnicianAssignment = flags.EnableTechnicianAssignment,
                    enableInventoryManagement = flags.EnableInventoryManagement,
                    enableReports = flags.EnableReports
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
