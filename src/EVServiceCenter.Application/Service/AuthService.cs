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

        public AuthService(
            IAccountService accountService,
            IAuthRepository authRepository,
            IAccountRepository accountRepository,
            IOtpCodeRepository otpRepository,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _accountService = accountService;
            _authRepository = authRepository;
            _accountRepository = accountRepository;
            _otpRepository = otpRepository;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<string> RegisterAsync(AccountRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required");
            if (string.IsNullOrWhiteSpace(request.PasswordHash))
                throw new Exception("Password is required");

            var existed = await _accountRepository.GetAccountByEmailAsync(request.Email);
            if (existed != null) throw new Exception("Email already exists");

            var hashed = BCryptNet.HashPassword(request.PasswordHash);

            var user = new User
            {
                Email = request.Email.Trim(),
                Username = string.IsNullOrWhiteSpace(request.UserName) ? request.Email.Trim() : request.UserName.Trim(),
                PasswordHash = hashed,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Address = request.Address,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Member" : request.Role,
                IsActive = true,
                EmailVerified = false,
                PhoneVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

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

            // Gửi mail
            var subject = "Xác minh tài khoản";
            var body = $@"
                <p>Xin chào {user.FullName ?? user.Email},</p>
                <p>Mã OTP xác minh tài khoản của bạn là: <b style=""font-size:18px"">{otp}</b></p>
                <p>Mã có hiệu lực trong 5 phút.</p>";
            await _emailService.SendEmailAsync(user.Email, subject, body);

            return "Đăng ký thành công. Vui lòng kiểm tra email để xác minh OTP.";
        }

        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var user = await _accountRepository.GetAccountByEmailAsync(email);
            if (user == null) throw new Exception("User not found");

            var lastOtp = await _otpRepository.GetLastOtpCodeAsync(user.UserId, "Register");
            if (lastOtp == null) return false;

            if (lastOtp.IsUsed || lastOtp.ExpiresAt < DateTime.UtcNow) return false;

            if (lastOtp.Otpcode1 != otp)
            {
                lastOtp.AttemptCount++;
                await _otpRepository.UpdateAsync(lastOtp);
                return false;
            }

            lastOtp.IsUsed = true;
            lastOtp.UsedAt = DateTime.UtcNow;
            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _otpRepository.UpdateAsync(lastOtp);
            await _accountRepository.UpdateAccountAsync(user);

            return true;
        }

        public async Task<string> LoginAsync(LoginRequest request)
        {
            var user = await _accountRepository.GetAccountByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("Invalid email or password");

            if (!BCryptNet.Verify(request.PasswordHash, user.PasswordHash))
                throw new Exception("Invalid email or password");

            if (!user.EmailVerified)
                throw new Exception("Email not verified");

            return GenerateJwt(user);
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
