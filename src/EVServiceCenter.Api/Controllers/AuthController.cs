using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICloudinaryService _cloudinaryService;

        public AuthController(IAuthService authService, ICloudinaryService cloudinaryService)
        {
            _authService = authService;
            _cloudinaryService = cloudinaryService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AccountRequest request)
        {
            // Kiểm tra ModelState validation (từ validation attributes)
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return BadRequest(new 
                { 
                    success = false,
                    message = "Dữ liệu đầu vào không hợp lệ",
                    errors = errors
                });
            }

            try
            {
                var result = await _authService.RegisterAsync(request);
                
                return Ok(new 
                { 
                    success = true,
                    message = result,
                    data = new
                    {
                        email = request.Email,
                        fullName = request.FullName,
                        registeredAt = DateTime.UtcNow
                    }
                });
            }
            catch (ArgumentException argEx)
            {
                // Business logic validation errors
                return BadRequest(new 
                { 
                    success = false,
                    message = "Lỗi validation",
                    errors = new[] { argEx.Message }
                });
            }
            catch (InvalidOperationException invEx)
            {
                // Database constraint errors
                return BadRequest(new 
                { 
                    success = false,
                    message = "Lỗi dữ liệu",
                    errors = new[] { invEx.Message }
                });
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                // Registration error occurred

                // Parse specific database errors
                var errorMessage = "Lỗi hệ thống";
                
                if (ex.Message.Contains("CK_Users_Gender"))
                {
                    errorMessage = "Giới tính phải là 'MALE' hoặc 'FEMALE'";
                }
                else if (ex.Message.Contains("UQ_Users_Email") || ex.Message.Contains("Email already exists"))
                {
                    errorMessage = "Email này đã được sử dụng. Vui lòng dùng email khác.";
                }
                else if (ex.Message.Contains("UQ_Users_PhoneNumber") || ex.Message.Contains("Phone number already exists"))
                {
                    errorMessage = "Số điện thoại này đã được sử dụng. Vui lòng dùng số khác.";
                }
                else if (ex.Message.Contains("CHECK constraint"))
                {
                    errorMessage = "Dữ liệu nhập vào không đúng định dạng yêu cầu của hệ thống.";
                }
                else if (ex.Message.Contains("FOREIGN KEY constraint"))
                {
                    errorMessage = "Dữ liệu tham chiếu không hợp lệ.";
                }
                else if (ex.Message.Contains("duplicate key"))
                {
                    errorMessage = "Thông tin này đã tồn tại trong hệ thống.";
                }
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình đăng ký",
                    errors = new[] { errorMessage },
                    // Include technical details for debugging (remove in production)
                    debug = ex.Message.Length > 200 ? ex.Message.Substring(0, 200) + "..." : ex.Message
                });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromQuery] string email, [FromQuery] string otp)
        {
            try
            {
                var ok = await _authService.VerifyOtpAsync(email, otp);
                return ok ? Ok(new { success = true, message = "Xác minh thành công" })
                          : BadRequest(new { success = false, message = "OTP không hợp lệ hoặc đã hết hạn" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                return BadRequest(new { success = false, message = "Gửi email thất bại. Vui lòng kiểm tra địa chỉ email và thử lại.", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Kiểm tra ModelState validation (từ validation attributes)
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return BadRequest(new 
                { 
                    success = false,
                    message = "Dữ liệu đầu vào không hợp lệ",
                    errors = errors
                });
            }

            try
            {
                var result = await _authService.LoginAsync(request);
                
                var message = "Đăng nhập thành công";
                if (!result.EmailVerified)
                {
                    message += ". Khuyến nghị: Hãy xác thực email để bảo mật tài khoản tốt hơn.";
                }

                // T?o response ph� h?p v?i FE
                var response = new
                {
                    success = true,
                    message = message,
                    data = new
                    {
                        token = result.AccessToken,
                        user = new
                        {
                            id = result.UserId,  // Thêm field 'id' để FE có thể sử dụng
                            userId = result.UserId,
                            customerId = result.CustomerId,
                            staffId = result.StaffId,
                            technicianId = result.TechnicianId,
                            email = result.Email ?? "",
                            fullName = result.FullName,
                            phoneNumber = result.PhoneNumber ?? "",
                            dateOfBirth = result.DateOfBirth,
                            address = result.Address ?? "",
                            gender = result.Gender ?? "",
                            avatarUrl = result.AvatarUrl ?? "",
                            role = result.Role,
                            isActive = result.IsActive,
                            emailVerified = result.EmailVerified,
                            createdAt = result.CreatedAt,
                            updatedAt = result.UpdatedAt
                        }
                    }
                };

                return Ok(response);
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new 
                { 
                    success = false,
                    message = "Đăng nhập thất bại",
                    errors = new[] { argEx.Message }
                });
            }
            catch (Exception ex)
            {
                // Login error occurred
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại sau.",
                    errors = new[] { ex.Message }
                });
            }
        }


        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            // Kiểm tra ModelState validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return BadRequest(new 
                { 
                    success = false,
                    message = "Dữ liệu đầu vào không hợp lệ",
                    errors = errors
                });
            }

            try
            {
                var result = await _authService.VerifyEmailAsync(request.UserId, request.OtpCode);
                
                return Ok(new 
                { 
                    success = true,
                    message = result,
                    data = new
                    {
                        userId = request.UserId,
                        verifiedAt = DateTime.UtcNow,
                        emailVerified = true
                    }
                });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new 
                { 
                    success = false,
                    message = "Lỗi xác thực",
                    errors = new[] { argEx.Message }
                });
            }
            catch (Exception ex)
            {
                // Email verification error occurred
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình xác thực email. Vui lòng thử lại sau.",
                    errors = new[] { ex.Message }
                });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            // Ki?m tra ModelState validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return BadRequest(new 
                { 
                    success = false,
                    message = "Dữ liệu đầu vào không hợp lệ",
                    errors = errors
                });
            }

            try
            {
                var result = await _authService.ResendVerificationEmailAsync(request.Email);
                
                return Ok(new 
                { 
                    success = true,
                    message = result,
                    data = new
                    {
                        email = request.Email,
                        resentAt = DateTime.UtcNow
                    }
                });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new 
                { 
                    success = false,
                    message = "Lỗi validation",
                    errors = new[] { argEx.Message }
                });
            }
            catch (Exception ex)
            {
                // Resend verification error occurred
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình gửi lại mã xác thực. Vui lòng thử lại sau.",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Yêu cầu đặt lại mật khẩu - gửi OTP về email
        /// </summary>
        [HttpPost("reset-password/request")]
        public async Task<IActionResult> RequestResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ", errors = errors });
                }

                var result = await _authService.RequestResetPasswordAsync(request.Email);
                return Ok(new { success = true, message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = "Lỗi xác thực", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận đặt lại mật khẩu - xác thực OTP và đổi mật khẩu
        /// </summary>
        [HttpPost("reset-password/confirm")]
        public async Task<IActionResult> ConfirmResetPassword([FromBody] ConfirmResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ", errors = errors });
                }

                var result = await _authService.ConfirmResetPasswordAsync(request);
                return Ok(new { success = true, message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = "Lỗi validation", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Đăng xuất - xóa refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc đã hết hạn." });
                }

                var result = await _authService.LogoutAsync(userId);
                return Ok(new { success = true, message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = "Lỗi xác thực", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin profile của user hiện tại
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc đã hết hạn." });
                }

                var result = await _authService.GetUserProfileAsync(userId);
                return Ok(new { success = true, message = "Lấy thông tin profile thành công", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = "Lỗi validation", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin profile của user hiện tại (không cho đổi email/phone)
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ", errors = errors });
                }

                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc đã hết hạn." });
                }

                var result = await _authService.UpdateUserProfileAsync(userId, request);
                return Ok(new { success = true, message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = "Lỗi validation", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Đổi mật khẩu cho user hiện tại
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ", errors = errors });
                }

                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc đã hết hạn." });
                }

                var result = await _authService.ChangePasswordAsync(userId, request);
                return Ok(new { success = true, message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = "Lỗi validation", errors = new[] { ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Upload avatar cho user hiện tại
        /// </summary>
        [HttpPost("upload-avatar")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                // Lấy UserId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc đã hết hạn." });
                }

                // Upload ảnh lên Cloudinary
                var avatarUrl = await _cloudinaryService.UploadImageAsync(file, "ev-service/avatars");

                // Cập nhật avatar URL trong database
                await _authService.UpdateUserAvatarAsync(userId, avatarUrl);

                return Ok(new { 
                    success = true, 
                    message = "Upload avatar thành công!",
                    data = new { 
                        avatarUrl = avatarUrl,
                        cloudinaryUrl = avatarUrl,
                        optimized = "Ảnh đã được tối ưu hóa tự động bởi Cloudinary"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = ex.Message,
                    suggestion = "Vui lòng kiểm tra định dạng và kích thước file ảnh."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống khi upload ảnh: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Đăng nhập bằng Google
        /// </summary>
        /// <param name="request">Google login request với token</param>
        /// <returns>JWT token và thông tin user</returns>
        [HttpPost("login-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
                }

                var result = await _authService.LoginWithGoogleAsync(request);
                
                // Tạo response phù hợp với FE
                var response = new
                {
                    success = true,
                    message = "Đăng nhập với Google thành công",
                    data = new
                    {
                        token = result.AccessToken,
                        user = new
                        {
                            id = result.UserId,  // Thêm field 'id' để FE có thể sử dụng
                            userId = result.UserId,
                            customerId = result.CustomerId,
                            staffId = result.StaffId,
                            technicianId = result.TechnicianId,
                            email = result.Email ?? "",
                            fullName = result.FullName,
                            phoneNumber = result.PhoneNumber ?? "",
                            dateOfBirth = result.DateOfBirth,
                            address = result.Address ?? "",
                            gender = result.Gender ?? "",
                            avatarUrl = result.AvatarUrl ?? "",
                            role = result.Role,
                            isActive = result.IsActive,
                            emailVerified = result.EmailVerified,
                            createdAt = result.CreatedAt,
                            updatedAt = result.UpdatedAt
                        }
                    }
                };
                
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
