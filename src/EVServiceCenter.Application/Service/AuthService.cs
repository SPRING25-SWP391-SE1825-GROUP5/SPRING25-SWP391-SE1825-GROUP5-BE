using System;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using BCrypt.Net;
using EVServiceCenter.Application.Models.Responses;




namespace EVServiceCenter.Application.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAccountService _accountService;
        private readonly IAuthRepository _authRepository;
        public AuthService(IAccountService accountService, IAuthRepository authRepository)
        {
            _accountService = accountService;
            _authRepository = authRepository;
        }
        public async Task<string> RegisterAsync(AccountRequest request)
        {
            if (!IsValidEmail(request.Email))
            {
                throw new Exception("Invalid email format.");
            }

            // CHeck if the password is valid
            if (!IsValidPassword(request.PasswordHash))
            {
                throw new Exception("Password must have at least 8 characters, including a special character, a number, an uppercase letter, and a lowercase letter.");
            }

            // Check if the phone number is valid
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                throw new Exception("Invalid phone number format. It should start with 0 or +84 and have 10 digits.");
            }

            var existedAccount = await _accountService.GetAccountByEmailAsync(request.Email);
            if (existedAccount != null)
                throw new Exception("Email already exists.");
            var account = new User
            {
                Username = request.UserName,
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash),
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                Role = "Customer",
            };
            await _authRepository.RegisterAsync(account);
            return "User registered successfully.";

        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _accountService.GetAccountByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid email or password.");

            return new LoginResponse
            {
                UserName = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth
            };
        }




        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Check email format using regex
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }

        // Check password is valid
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            var passwordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
            return passwordRegex.IsMatch(password);
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Loại bỏ khoảng trắng
            phoneNumber = phoneNumber.Replace(" ", "");
            var phoneRegex = new Regex(@"^(?:\+84|0)(?:\d{9})$");
            return phoneRegex.IsMatch(phoneNumber);
        }


    }
}
