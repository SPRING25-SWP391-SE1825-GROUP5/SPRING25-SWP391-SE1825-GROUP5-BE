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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AccountRequest request)
        {
            // Kiểm tra ModelState validation (từ validation attributes)
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value.Errors)
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
                Console.WriteLine($"Registration error: {ex}");
                
                // Parse specific database errors
                var errorMessage = "Lỗi hệ thống";
                
                if (ex.Message.Contains("CK_Users_Gender"))
                {
                    errorMessage = "Giới tính phải là 'MALE' hoặc 'FEMALE'";
                }
                else if (ex.Message.Contains("UQ_Users_Email") || ex.Message.Contains("Email already exists"))
                {
                    errorMessage = "Email này đã được sử dụng. Vui lòng sử dụng email khác.";
                }
                else if (ex.Message.Contains("UQ_Users_PhoneNumber") || ex.Message.Contains("Phone number already exists"))
                {
                    errorMessage = "Số điện thoại này đã được sử dụng. Vui lòng sử dụng số khác.";
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Kiểm tra ModelState validation (từ validation attributes)
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value.Errors)
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
                
                return Ok(new 
                { 
                    success = true,
                    message = "Đăng nhập thành công",
                    data = result
                });
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
                Console.WriteLine($"Login error: {ex}");
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại sau.",
                    errors = new[] { "Lỗi hệ thống" }
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
                    .SelectMany(x => x.Value.Errors)
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
                Console.WriteLine($"Email verification error: {ex}");
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình xác thực email. Vui lòng thử lại sau.",
                    errors = new[] { "Lỗi hệ thống" }
                });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            // Kiểm tra ModelState validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value.Errors)
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
                Console.WriteLine($"Resend verification error: {ex}");
                
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình gửi lại mã xác thực. Vui lòng thử lại sau.",
                    errors = new[] { "Lỗi hệ thống" }
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
                return BadRequest(new { success = false, message = "Lỗi validation", errors = new[] { ex.Message } });
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
                return BadRequest(new { success = false, message = "Lỗi validation", errors = new[] { ex.Message } });
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
        [Authorize]
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

                // Validate file
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn file ảnh." });

                // Validate file type - chỉ hỗ trợ JPG và PNG
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Định dạng file không được hỗ trợ. Vui lòng chọn file ảnh có định dạng JPG hoặc PNG.",
                        supportedFormats = new[] { "JPG", "PNG" },
                        currentFormat = fileExtension.ToUpper(),
                        suggestion = "Hãy chuyển đổi file sang định dạng JPG hoặc PNG và thử lại."
                    });
                }

                // Validate file size (max 4MB)
                if (file.Length > 4 * 1024 * 1024)
                {
                    var fileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2);
                    return BadRequest(new { 
                        success = false, 
                        message = $"Kích thước file quá lớn. File hiện tại: {fileSizeMB}MB, tối đa cho phép: 4MB.",
                        currentSize = $"{fileSizeMB}MB",
                        maxSize = "4MB",
                        suggestion = "Vui lòng nén ảnh hoặc chọn ảnh có kích thước nhỏ hơn 4MB."
                    });
                }

                // Tạo tên file unique
                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var filePath = Path.Combine(uploadsPath, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Tạo URL để truy cập file
                var avatarUrl = $"/uploads/avatars/{fileName}";

                // Cập nhật avatar URL trong database
                // Cập nhật avatar URL trực tiếp vào database
                await _authService.UpdateUserAvatarAsync(userId, avatarUrl);

                return Ok(new { 
                    success = true, 
                    message = "Upload avatar thành công!",
                    data = new { avatarUrl = avatarUrl }
                });
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
    }
}
