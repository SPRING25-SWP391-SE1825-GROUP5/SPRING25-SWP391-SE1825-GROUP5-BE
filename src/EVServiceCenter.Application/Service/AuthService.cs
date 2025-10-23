using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.IRepositories;
using BCrypt.Net;
using EVServiceCenter.Application.Models.Responses;
using Google.Apis.Auth;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
namespace EVServiceCenter.Application.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAccountService _accountService;
        private readonly IAuthRepository _authRepository;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IOtpCodeRepository _otpRepository;
        private readonly IMemoryCache _cache;
        private readonly ILoginLockoutService _loginLockoutService;
        
        public AuthService(IAccountService accountService, IAuthRepository authRepository, IEmailService emailService, IOtpService otpService, IJwtService jwtService, IConfiguration configuration, ICustomerRepository customerRepository, IAccountRepository accountRepository, IOtpCodeRepository otpRepository, IMemoryCache cache, ILoginLockoutService loginLockoutService)
        {
            _accountService = accountService;
            _authRepository = authRepository;
            _emailService = emailService;
            _otpService = otpService;
            _jwtService = jwtService;
            _configuration = configuration;
            _customerRepository = customerRepository;
            _accountRepository = accountRepository;
            _otpRepository = otpRepository;
            _cache = cache;
            _loginLockoutService = loginLockoutService;
        }
        private bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
                
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) 
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
        
        public async Task<string> RegisterAsync(AccountRequest request)
        {
            // Validation chi tiết từng trường
            await ValidateRegistrationRequestAsync(request);

            try
            {
                // Tạo user entity
                var user = new User
                {
                    Email = request.Email.ToLower().Trim(),
                    FullName = request.FullName.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Address = !string.IsNullOrWhiteSpace(request.Address) ? request.Address.Trim() : null,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    AvatarUrl = !string.IsNullOrWhiteSpace(request.AvatarUrl) ? request.AvatarUrl.Trim() : null,
                    Role = "CUSTOMER", // Luôn là CUSTOMER cho đăng ký công khai
                    IsActive = true, // Tài khoản mặc định là active
                    EmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Lưu user vào database
                await _authRepository.RegisterAsync(user);

                // Tạo Customer record tương ứng
                var customer = new Customer
                {
                    UserId = user.UserId,
                    IsGuest = false
                };

                await _customerRepository.CreateCustomerAsync(customer);

                // Tạo và gửi mã OTP xác thực email
                try
                {
                    var otpCode = await _otpService.CreateOtpAsync(user.UserId, user.Email, "EMAIL_VERIFICATION");
                    await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, otpCode);
                    
                    return "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để nhận mã xác thực và hoàn tất kích hoạt tài khoản.";
                }
                catch (Exception)
                {
                    // Nếu gửi OTP thất bại, vẫn coi đăng ký thành công nhưng yêu cầu user thử lại
                    // OTP email sending failed - continuing without email
                    return "Đăng ký tài khoản thành công! Tuy nhiên có lỗi khi gửi email xác thực. Vui lòng thử yêu cầu gửi lại mã xác thực.";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình đăng ký: {ex.Message}");
            }
        }

        private async Task ValidateRegistrationRequestAsync(AccountRequest request)
        {
            var errors = new List<string>();

            // Kiểm tra email format
            if (!IsValidEmail(request.Email))
            {
                errors.Add("Email không đúng định dạng");
            }

            // Kiểm tra password strength
            if (!IsValidPassword(request.Password))
            {
                errors.Add("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
            }

            // Kiểm tra password confirm
            if (request.Password != request.ConfirmPassword)
            {
                errors.Add("Mật khẩu xác nhận không khớp.");
            }

            // Kiểm tra phone number format
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                errors.Add("Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.");
            }

            // Kiểm tra tuổi tối thiểu
            if (!IsValidAge(request.DateOfBirth))
            {
                errors.Add("Phải đủ 16 tuổi trở lên để đăng ký tài khoản.");
            }

            // Kiểm tra giới tính hợp lệ
            if (!IsValidGender(request.Gender))
            {
                errors.Add("Giới tính phải là MALE hoặc FEMALE.");
            }

            // Kiểm tra email đã tồn tại
            var existingUserByEmail = await _accountService.GetAccountByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                errors.Add("Email này đã được sử dụng. Vui lòng dùng email khác.");
            }

            // Kiểm tra số điện thoại đã tồn tại
            var existingUserByPhone = await _accountService.GetAccountByPhoneNumberAsync(request.PhoneNumber);
            if (existingUserByPhone != null)
            {
                errors.Add("Số điện thoại này đã được sử dụng. Vui lòng dùng số điện thoại khác.");
            }

            // Throw exception với tất cả lỗi nếu có
            if (errors.Any())
            {
                throw new ArgumentException(string.Join(" ", errors));
            }
        }

        private bool IsValidAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = CalculateAge(dateOfBirth, today);
            return age >= 16;
        }

        private int CalculateAge(DateOnly birthDate, DateOnly today)
        {
            var age = today.Year - birthDate.Year;
            if (birthDate.Month > today.Month || 
                (birthDate.Month == today.Month && birthDate.Day > today.Day))
            {
                age--;
            }
            return age;
        }

        private bool IsValidGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                return false;

            var validGenders = new[] { "MALE", "FEMALE" };
            return validGenders.Contains(gender, StringComparer.Ordinal);
        }

        public async Task<LoginTokenResponse> LoginAsync(LoginRequest request)
        {
            User? user = null;
            string email = request.Email;

            // Chỉ sử dụng email để đăng nhập
            user = await _accountService.GetAccountByEmailAsync(email);

            // Kiểm tra lockout trước khi xử lý login
            if (!string.IsNullOrEmpty(email))
            {
                var isLocked = await _loginLockoutService.IsAccountLockedAsync(email);
                if (isLocked)
                {
                    var lockoutExpiry = await _loginLockoutService.GetLockoutExpiryAsync(email);
                    var remainingTime = lockoutExpiry?.Subtract(DateTime.UtcNow) ?? TimeSpan.Zero;
                    throw new ArgumentException($"Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau {remainingTime.Minutes} phút {remainingTime.Seconds} giây.");
                }
            }

            if (user == null)
            {
                // Ghi nhận lần thử sai nếu có email
                if (!string.IsNullOrEmpty(email))
                {
                    await _loginLockoutService.RecordFailedAttemptAsync(email);
                }
                throw new ArgumentException("Email hoặc mật khẩu không đúng");
            }

            // Note: Email verification is optional - users can login without verification
            // Just log for tracking purposes
            if (!user.EmailVerified)
            {
                // User logged in without email verification
            }
            
            // Kiểm tra tài khoản có active không
            if (!user.IsActive)
                throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên");

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Ghi nhận lần thử sai
                await _loginLockoutService.RecordFailedAttemptAsync(user.Email);
                
                var remainingAttempts = await _loginLockoutService.GetRemainingAttemptsAsync(user.Email);
                if (remainingAttempts > 0)
                {
                    throw new ArgumentException($"Email hoặc mật khẩu không đúng. Còn {remainingAttempts} lần thử.");
                }
                else
                {
                    var lockoutExpiry = await _loginLockoutService.GetLockoutExpiryAsync(user.Email);
                    var remainingTime = lockoutExpiry?.Subtract(DateTime.UtcNow) ?? TimeSpan.Zero;
                    throw new ArgumentException($"Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau {remainingTime.Minutes} phút {remainingTime.Seconds} giây.");
                }
            }

            // Xóa failed attempts khi login thành công
            await _loginLockoutService.ClearFailedAttemptsAsync(user.Email);

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = _jwtService.GetTokenExpiration();
            var expiresIn = _jwtService.GetTokenExpirationInSeconds();

            // Update refresh token in database
            user.RefreshToken = Encoding.UTF8.GetBytes(refreshToken);
            user.UpdatedAt = DateTime.UtcNow;
            await _authRepository.UpdateUserAsync(user);

            // Lấy CustomerId từ database
            int? customerId = null;
            if (user.Role == "CUSTOMER")
            {
                var customer = await _customerRepository.GetCustomerByUserIdAsync(user.UserId);
                customerId = customer?.CustomerId;
            }

            return new LoginTokenResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken,
                UserId = user.UserId,
                CustomerId = customerId,
                FullName = user.FullName,
                Role = user.Role ?? "CUSTOMER",
                EmailVerified = user.EmailVerified,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                Gender = user.Gender,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<string> VerifyEmailAsync(int userId, string otpCode)
        {
            try
            {
            // Kiểm tra user có tồn tại không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                if (user.EmailVerified)
                    return "Email đã được xác thực trước đó.";

                // Xác thực OTP
                var isValidOtp = await _otpService.VerifyOtpAsync(userId, otpCode, "EMAIL_VERIFICATION");
                if (!isValidOtp)
                {
                    // Tăng số lần thử sai
                    await _otpService.IncrementAttemptCountAsync(userId, otpCode, "EMAIL_VERIFICATION");
                    throw new ArgumentException("Mã xác thực không đúng hoặc đã hết hạn. Vui lòng thử lại hoặc yêu cầu mã mới.");
                }

                // Cập nhật trạng thái email verified và active tài khoản
                await _authRepository.UpdateEmailVerifiedStatusAsync(userId, true);
                await _authRepository.UpdateUserActiveStatusAsync(userId, true);

                // Gửi email chào mừng
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
                }
                catch (Exception)
                {
                    // Welcome email sending failed - continuing without email
                    // Không throw exception vì xác thực đã thành công
                }

                return "Xác thực email thành công! Chào mừng bạn đến với EV Service Center.";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình xác thực email: {ex.Message}");
            }
        }

        public async Task<string> ResendVerificationEmailAsync(string email)
        {
            try
            {
                var user = await _accountService.GetAccountByEmailAsync(email);
                if (user == null)
                    throw new ArgumentException("Email không tồn tại trong hệ thống.");

                if (user.EmailVerified)
                    return "Email đã được xác thực. Không cần gửi lại mã.";

                // Kiểm tra có thể tạo OTP mới không (chống spam)
                var canCreateNew = await _otpService.CanCreateNewOtpAsync(user.UserId, "EMAIL_VERIFICATION");
                if (!canCreateNew)
                    throw new ArgumentException("Vui lòng chờ 2 phút trước khi yêu cầu mã xác thực mới.");

                // Tạo và gửi OTP mới
                var otpCode = await _otpService.CreateOtpAsync(user.UserId, user.Email, "EMAIL_VERIFICATION");
                await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, otpCode);

                return "Mã xác thực mới đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã trong vòng 15 phút.";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình gửi lại mã xác thực: {ex.Message}");
            }
        }

        public async Task<string> RequestResetPasswordAsync(string email)
        {
            try
            {
                // Validate email format
                if (!IsValidEmail(email))
                    throw new ArgumentException("Email không hợp lệ");

                // Kiểm tra user có tồn tại không
                var user = await _accountService.GetAccountByEmailAsync(email);
                if (user == null)
                    throw new ArgumentException("Email không tồn tại trong hệ thống.");

                if (!user.IsActive)
                    throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                // Kiểm tra có thể tạo OTP mới không (chống spam)
                var canCreateNew = await _otpService.CanCreateNewOtpAsync(user.UserId, "PASSWORD_RESET");
                if (!canCreateNew)
                    throw new ArgumentException("Vui lòng chờ 2 phút trước khi yêu cầu mã đặt lại mật khẩu mới.");

                // Tạo và gửi OTP reset password
                var otpCode = await _otpService.CreateOtpAsync(user.UserId, user.Email, "PASSWORD_RESET");
                await _emailService.SendResetPasswordEmailAsync(user.Email, user.FullName, otpCode);

                return "Mã xác thực đặt lại mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã trong vòng 15 phút.";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình yêu cầu đặt lại mật khẩu: {ex.Message}");
            }
        }

        public async Task<string> ConfirmResetPasswordAsync(ConfirmResetPasswordRequest request)
        {
            try
            {
                // Validate email format
                if (!IsValidEmail(request.Email))
                    throw new ArgumentException("Email không hợp lệ");

                // Validate password strength
                if (!IsValidPassword(request.NewPassword))
                    throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");

                // Validate password confirm
                if (request.NewPassword != request.ConfirmPassword)
                    throw new ArgumentException("Mật khẩu xác nhận không khớp.");

                // Kiểm tra user có tồn tại không
                var user = await _accountService.GetAccountByEmailAsync(request.Email);
                if (user == null)
                    throw new ArgumentException("Email không tồn tại trong hệ thống.");

                if (!user.IsActive)
                    throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                // Xác thực OTP
                var isValidOtp = await _otpService.VerifyOtpAsync(user.UserId, request.OtpCode, "PASSWORD_RESET");
                if (!isValidOtp)
                {
                    // Tăng số lần thử sai
                    await _otpService.IncrementAttemptCountAsync(user.UserId, request.OtpCode, "PASSWORD_RESET");
                    throw new ArgumentException("Mã xác thực không đúng hoặc đã hết hạn. Vui lòng thử lại hoặc yêu cầu mã mới.");
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập với mật khẩu mới.";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình đặt lại mật khẩu: {ex.Message}");
            }
        }

        public async Task<string> LogoutAsync(int userId)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                // Xóa refresh token
                user.RefreshToken = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return "Đăng xuất thành công.";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình đăng xuất: {ex.Message}");
            }
        }

        public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                return new UserProfileResponse
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address ?? string.Empty,
                    Gender = user.Gender ?? string.Empty,
                    AvatarUrl = user.AvatarUrl ?? string.Empty,
                    Role = user.Role ?? "CUSTOMER",
                    IsActive = user.IsActive,
                    EmailVerified = user.EmailVerified,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình lấy thông tin profile: {ex.Message}");
            }
        }

        public async Task<string> UpdateUserProfileAsync(int userId, UpdateProfileRequest request)
        {
            try
            {
                // Validate age
                if (!IsValidAge(request.DateOfBirth))
                    throw new ArgumentException("Phải đủ 16 tuổi trở lên.");

                // Validate gender
                if (!IsValidGender(request.Gender))
                    throw new ArgumentException("Giới tính phải là MALE hoặc FEMALE.");

                // Kiểm tra user có tồn tại không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                if (!user.IsActive)
                    throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                // Cập nhật thông tin
                user.FullName = request.FullName.Trim();
                user.DateOfBirth = request.DateOfBirth;
                user.Gender = request.Gender;
                user.Address = !string.IsNullOrWhiteSpace(request.Address) ? request.Address.Trim() : string.Empty;

                // Cho phép cập nhật email nếu cung cấp và khác hiện tại
                if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingByEmail = await _accountService.GetAccountByEmailAsync(request.Email);
                    if (existingByEmail != null && existingByEmail.UserId != user.UserId)
                        throw new ArgumentException("Email này đã được sử dụng. Vui lòng dùng email khác.");
                    user.Email = request.Email.ToLower().Trim();
                    user.EmailVerified = false; // yêu cầu xác thực lại nếu đổi email
                }

                // Cho phép cập nhật số điện thoại nếu cung cấp và khác hiện tại
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !string.Equals(request.PhoneNumber, user.PhoneNumber, StringComparison.Ordinal))
                {
                    var existingByPhone = await _accountService.GetAccountByPhoneNumberAsync(request.PhoneNumber);
                    if (existingByPhone != null && existingByPhone.UserId != user.UserId)
                        throw new ArgumentException("Số điện thoại này đã được sử dụng. Vui lòng dùng số khác.");
                    user.PhoneNumber = request.PhoneNumber;
                }
                user.UpdatedAt = DateTime.UtcNow;

                await _authRepository.UpdateUserAsync(user);

                return "Cập nhật thông tin cá nhân thành công!";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình cập nhật profile: {ex.Message}");
            }
        }

        public async Task<string> UpdateUserAvatarAsync(int userId, string avatarUrl)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                if (!user.IsActive)
                    throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                // Cập nhật avatar URL
                user.AvatarUrl = avatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _authRepository.UpdateUserAsync(user);

                return "Cập nhật avatar thành công!";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình cập nhật avatar: {ex.Message}");
            }
        }

        public async Task<string> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            try
            {
                // Validate password strength
                if (!IsValidPassword(request.NewPassword))
                    throw new ArgumentException("Mật khẩu mới phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");

                // Validate password confirm
                if (request.NewPassword != request.ConfirmNewPassword)
                    throw new ArgumentException("Xác nhận mật khẩu mới không khớp.");

                // Kiểm tra user có tồn tại không
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                if (!user.IsActive)
                    throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    throw new ArgumentException("Mật khẩu hiện tại không đúng.");

                // Kiểm tra mật khẩu mới khác mật khẩu cũ
                if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
                    throw new ArgumentException("Mật khẩu mới phải khác mật khẩu hiện tại.");

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return "Đổi mật khẩu thành công! Vui lòng đăng nhập lại với mật khẩu mới.";
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình đổi mật khẩu: {ex.Message}");
            }
        }



        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var user = await _accountRepository.GetAccountByEmailAsync(email);
            if (user == null) throw new Exception("Người dùng không tồn tại.");

            var lastOtp = await _otpRepository.GetLastOtpCodeAsync(user.UserId, "EMAIL_VERIFICATION");
            if (lastOtp == null) throw new ArgumentException("Không tìm thấy mã OTP.");

            if (lastOtp.IsUsed || lastOtp.ExpiresAt < DateTime.UtcNow)
                throw new ArgumentException("Mã OTP đã hết hạn hoặc đã được sử dụng.");

            if (lastOtp.Otpcode1 != otp)
            {
                lastOtp.AttemptCount++;
                await _otpRepository.UpdateAsync(lastOtp);
                throw new ArgumentException("Mã OTP không chính xác.");
            }

            lastOtp.IsUsed = true;
            lastOtp.UsedAt = DateTime.UtcNow;

            user.EmailVerified = true;
            // IsActive đã được set = true từ lúc đăng ký, không cần set lại
            user.UpdatedAt = DateTime.UtcNow;

            await _otpRepository.UpdateAsync(lastOtp);
            await _accountRepository.UpdateAccountAsync(user);

            return true;
        }


        public async Task<LoginTokenResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Token))
                    throw new ArgumentException("Google token là bắt buộc");

                // Get Google ClientId from configuration
                var clientId = _configuration["Google:ClientId"];
                if (string.IsNullOrEmpty(clientId))
                    throw new InvalidOperationException("Chưa cấu hình Google:ClientId");

                // Validate Google ID token
                GoogleJsonWebSignature.Payload payload;
                try
                {
                    var settings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId }
                    };
                    payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Token Google không hợp lý: {ex.Message}");
                }

                if (payload == null || string.IsNullOrEmpty(payload.Email))
                    throw new ArgumentException("Không đọc được thông tin email từ Google");

                // Accept all Google OAuth emails (Gmail, Google Workspace, etc.)
                // Removed Gmail-only restriction for better user experience

                // Find or create user
                var email = payload.Email.ToLowerInvariant();
                var user = await _accountService.GetAccountByEmailAsync(email);
                
                if (user == null)
                {
                    // Create new user with Google login
                    var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 10) + "Aa1!";
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(randomPassword);

                    user = new User
                    {
                        Email = email,
                        PasswordHash = passwordHash,
                        FullName = string.IsNullOrWhiteSpace(payload.Name) ? email.Split('@')[0] : payload.Name,
                        PhoneNumber = "0000000000", // Default phone for Google users
                        DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-18)), // Default age
                        Gender = "MALE", // Default gender
                        Address = null,
                        AvatarUrl = payload.Picture ?? string.Empty,
                        Role = "Customer",
                        IsActive = true,
                        EmailVerified = payload.EmailVerified,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _authRepository.RegisterAsync(user);
                }
                else
                {
                    // Update existing user if needed
                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _authRepository.UpdateUserAsync(user);
                    }

                    // Update avatar if Google has a newer one
                    if (!string.IsNullOrEmpty(payload.Picture) && user.AvatarUrl != payload.Picture)
                    {
                        user.AvatarUrl = payload.Picture;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _authRepository.UpdateUserAsync(user);
                    }
                }

                // Check if account is active
                if (!user.IsActive)
                    throw new ArgumentException("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên");

                // Generate JWT tokens
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = _jwtService.GetTokenExpiration();
                var expiresIn = _jwtService.GetTokenExpirationInSeconds();

                // Update refresh token in database
                user.RefreshToken = Encoding.UTF8.GetBytes(refreshToken);
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                // Lấy CustomerId từ database
                int? customerId = null;
                if (user.Role == "CUSTOMER")
                {
                    var customer = await _customerRepository.GetCustomerByUserIdAsync(user.UserId);
                    customerId = customer?.CustomerId;
                }

                return new LoginTokenResponse
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer",
                    ExpiresIn = expiresIn,
                    ExpiresAt = expiresAt,
                    RefreshToken = refreshToken,
                    UserId = user.UserId,
                    CustomerId = customerId,
                    FullName = user.FullName,
                    Role = user.Role ?? "CUSTOMER",
                    EmailVerified = user.EmailVerified,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address,
                    Gender = user.Gender,
                    AvatarUrl = user.AvatarUrl,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình đăng nhập Google: {ex.Message}");
            }
        }

        // Token-based first-time password set (for guest -> user conversion)
        public async Task<string> SetPasswordWithTokenAsync(string token, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Token không hợp lý");
                if (!IsValidPassword(newPassword))
                    throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");

                // Decode simple GUID token from cache/OTP repo (reuse PASSWORD_RESET channel)
                // Expect token format: email-guid or just guid mapped to userId in OTP table
                var otp = await _otpRepository.GetByRawTokenAsync(token);
                if (otp == null || otp.ExpiresAt < DateTime.UtcNow || otp.IsUsed)
                    throw new ArgumentException("Token không hợp lý hoặc đã hết hạn");

                var user = await _authRepository.GetUserByIdAsync(otp.UserId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                user.EmailVerified = true; // consider verified after setting password via email link
                await _authRepository.UpdateUserAsync(user);

                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
                await _otpRepository.UpdateAsync(otp);

                return "Đặt mật khẩu thành công. Bạn có thể đăng nhập ngay.";
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi đặt mật khẩu: {ex.Message}");
            }
        }

        private string GenerateCustomerCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"CUS{timestamp}{random}";
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Loại bỏ tất cả ký tự không phải số
            var normalized = Regex.Replace(phoneNumber, @"[^\d]", "");
            
            // Nếu bắt đầu bằng 0, giữ nguyên
            if (normalized.StartsWith("0"))
                return normalized;
            
            // Nếu bắt đầu bằng 84, thay thế bằng 0
            if (normalized.StartsWith("84"))
                return "0" + normalized.Substring(2);
            
            // Nếu không có prefix, thêm 0
            return "0" + normalized;
        }

        private string GenerateJwt(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "Member"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email);
        }

        private bool IsValidPassword(string password)
        {
            // Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
            return passwordRegex.IsMatch(password);
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Số điện thoại phải bắt đầu bằng 0 và có đúng 10 số
            var phoneRegex = new Regex(@"^0\d{9}$");
            return phoneRegex.IsMatch(phoneNumber);
        }
    }
}
