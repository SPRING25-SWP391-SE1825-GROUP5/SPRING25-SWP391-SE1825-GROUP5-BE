using System;
using System.Text;
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
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
namespace EVServiceCenter.Application.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAccountService _accountService;
        private readonly IAuthRepository _authRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IOtpCodeRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        // cấu hình lock
        private const int MAX_FAILED_ATTEMPTS = 5;   // số lần nhập sai tối đa
        private static readonly TimeSpan LOCK_DURATION = TimeSpan.FromMinutes(10); // thời gian khóa


        public AuthService(
            IAccountService accountService,
            IAuthRepository authRepository,
            IAccountRepository accountRepository,
            IOtpCodeRepository otpRepository,
            IEmailService emailService,
            IConfiguration configuration, IMemoryCache cache)
        {
            _accountService = accountService;
            _authRepository = authRepository;
            _accountRepository = accountRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _configuration = configuration;
            _cache = cache;
        }
        private static string FailKey(string email) => $"login:fail:{email.ToLower()}";
        private static string LockKey(string email) => $"login:lock:{email.ToLower()}";

        public async Task<string> RegisterAsync(AccountRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) &&
        string.IsNullOrWhiteSpace(request.PasswordHash) &&
        string.IsNullOrWhiteSpace(request.UserName) &&
        string.IsNullOrWhiteSpace(request.FullName) &&
        string.IsNullOrWhiteSpace(request.PhoneNumber) &&
        request.DateOfBirth == null &&
        string.IsNullOrWhiteSpace(request.Address))
            {
                throw new Exception("Vui lòng nhập đầy đủ thông tin để đăng ký.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email không được để trống.");
            if (string.IsNullOrWhiteSpace(request.PasswordHash))
                throw new Exception("Mật khẩu không được để trống.");

            // Kiểm tra username 6–30 ký tự
            if (string.IsNullOrWhiteSpace(request.UserName) || request.UserName.Length < 6 || request.UserName.Length > 30)
                throw new Exception("Tên đăng nhập phải từ 6 đến 30 ký tự.");

            // Fullname chỉ chứa chữ (có dấu cũng ok), không số và ký tự đặc biệt
            if (string.IsNullOrWhiteSpace(request.FullName) || !System.Text.RegularExpressions.Regex.IsMatch(request.FullName, @"^[\p{L}\s]+$"))
                throw new Exception("Họ tên chỉ được chứa ký tự chữ và khoảng trắng.");

            // PhoneNumber: bắt đầu bằng 0 và có đúng 10 số
            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || !System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^0\d{9}$"))
                throw new Exception("Số điện thoại phải bắt đầu bằng số 0 và có 10 chữ số.");

            // Ngày sinh phải đủ 16 tuổi
            if (request.DateOfBirth == null)
                throw new Exception("Vui lòng nhập ngày sinh.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dob = request.DateOfBirth.Value;
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;

            if (age < 16)
                throw new Exception("Ngày sinh không hợp lệ.");

            // Kiểm tra email đã tồn tại
            var existed = await _accountRepository.GetAccountByEmailAsync(request.Email);
            if (existed != null) throw new Exception("Email đã được sử dụng.");

            // Hash password
            var hashed = BCryptNet.HashPassword(request.PasswordHash);

            var user = new User
            {
                Email = request.Email.Trim(),
                Username = request.UserName.Trim(),
                PasswordHash = hashed,
                FullName = request.FullName.Trim(),
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = dob,
                Address = request.Address,
                Role = "Customer", // luôn mặc định Customer
                IsActive = false,
                EmailVerified = false,
                PhoneVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Chỉ lưu khi validate xong
            await _accountRepository.CreateAccountAsync(user);

            // Sinh OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var otpEntity = new Otpcode
            {
                UserId = user.UserId,
                Otpcode1 = otp,
                Otptype = "Register",
                ContactInfo = user.Email,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _otpRepository.CreateOtpCodeAsync(otpEntity);

            // Gửi email xác minh
            var subject = "Xác minh tài khoản";
            var body = $@"
        <p>Xin chào {user.FullName},</p>
        <p>Mã OTP xác minh tài khoản của bạn là: <b style=""font-size:18px"">{otp}</b></p>
        <p>Mã có hiệu lực trong 5 phút.</p>";
            await _emailService.SendEmailAsync(user.Email, subject, body);

            return "Đăng ký thành công. Vui lòng kiểm tra email để xác minh OTP.";
        }



        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var user = await _accountRepository.GetAccountByEmailAsync(email);
            if (user == null) throw new Exception("Người dùng không tồn tại.");

            var lastOtp = await _otpRepository.GetLastOtpCodeAsync(user.UserId, "Register");
            if (lastOtp == null) throw new Exception("Không tìm thấy mã OTP.");

            if (lastOtp.IsUsed || lastOtp.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Mã OTP đã hết hạn hoặc đã được sử dụng.");

            if (lastOtp.Otpcode1 != otp)
            {
                lastOtp.AttemptCount++;
                await _otpRepository.UpdateAsync(lastOtp);
                throw new Exception("Mã OTP không chính xác.");
            }

            lastOtp.IsUsed = true;
            lastOtp.UsedAt = DateTime.UtcNow;

            user.EmailVerified = true;
            user.IsActive = true; 
            user.UpdatedAt = DateTime.UtcNow;

            await _otpRepository.UpdateAsync(lastOtp);
            await _accountRepository.UpdateAccountAsync(user);

            return true;
        }


        public async Task<string> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PasswordHash) && string.IsNullOrWhiteSpace(request.PasswordHash))
                throw new Exception("Vui lòng nhập email và mật khẩu.");

            var email = request.Email.Trim().ToLower();

            // 1) Kiểm tra có đang bị khóa không
            if (_cache.TryGetValue<DateTime>(LockKey(email), out var lockedUntil))
            {
                if (lockedUntil > DateTime.UtcNow)
                {
                    var minutesLeft = Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalMinutes);
                    throw new Exception($"Tài khoản tạm thời bị khóa do nhập sai quá nhiều. Vui lòng thử lại sau {minutesLeft} phút.");
                }
                else
                {
                    // hết khóa -> xóa trạng thái
                    _cache.Remove(LockKey(email));
                    _cache.Remove(FailKey(email));
                }
            }

            var user = await _accountRepository.GetAccountByEmailAsync(email);
            // Không tiết lộ thông tin tồn tại hay không, dùng thông báo chung
            if (user == null)
            {
                RegisterFailedAttempt(email);
                throw new Exception("Email hoặc mật khẩu không đúng.");
            }

            // 2) Kiểm tra mật khẩu
            var plainPassword = request.PasswordHash ?? request.PasswordHash; // tùy DTO của bạn
            if (!BCryptNet.Verify(plainPassword, user.PasswordHash))
            {
                RegisterFailedAttempt(email);
                throw new Exception("Email hoặc mật khẩu không đúng.");
            }

            // 3) Kiểm tra xác minh email
            if (!user.EmailVerified)
                throw new Exception("Email chưa được xác minh. Vui lòng kiểm tra hộp thư để xác minh.");

            // 4) Đăng nhập thành công -> reset đếm sai
            _cache.Remove(FailKey(email));
            _cache.Remove(LockKey(email));

            // 5) Trả JWT
            return GenerateJwt(user);
        }

        private void RegisterFailedAttempt(string email)
        {
            var key = FailKey(email);
            int current = 0;
            if (_cache.TryGetValue<int>(key, out var fails))
            {
                current = fails;
            }
            current++;

            // Lưu lại số lần fail, set TTL nhẹ (ví dụ 30 phút) để không giữ mãi
            _cache.Set(key, current, TimeSpan.FromMinutes(30));

            if (current >= MAX_FAILED_ATTEMPTS)
            {
                // Đặt khóa
                _cache.Set(LockKey(email), DateTime.UtcNow.Add(LOCK_DURATION), LOCK_DURATION);
            }
        }


        private string GenerateJwt(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
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
    }
}
