using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;

namespace EVServiceCenter.Application.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;

        public CustomerService(ICustomerRepository customerRepository, IAccountRepository accountRepository, IEmailService emailService)
        {
            _customerRepository = customerRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
        }

        public async Task<CustomerResponse> GetCurrentCustomerAsync(int userId)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
                if (customer == null)
                    throw new ArgumentException("Khách hàng không tồn tại.");

                return MapToCustomerResponse(customer);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khách hàng: {ex.Message}");
            }
        }

        public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreateCustomerRequestAsync(request);

                // Normalize phone number
                var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

                // Create customer entity
                var customer = new Customer
                {
                    UserId = null, // Will be set when user registers
                    IsGuest = request.IsGuest,
                    
                };

                // Save customer
                var createdCustomer = await _customerRepository.CreateCustomerAsync(customer);

                return MapToCustomerResponse(createdCustomer);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo khách hàng: {ex.Message}");
            }
        }

        public async Task<CustomerResponse> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
        {
            try
            {
                // Validate customer exists
                var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    throw new ArgumentException("Khách hàng không tồn tại.");

                // Validate request
                await ValidateUpdateCustomerRequestAsync(request, customerId);

                // Normalize phone number
                var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

                // Update customer
                customer.IsGuest = request.IsGuest;
                

                await _customerRepository.UpdateCustomerAsync(customer);

                return MapToCustomerResponse(customer);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật khách hàng: {ex.Message}");
            }
        }

        public async Task<CustomerResponse> QuickCreateCustomerAsync(QuickCreateCustomerRequest request)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("Họ tên không được trống");
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email không được trống");
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new ArgumentException("Số điện thoại không được trống");

            var email = request.Email.Trim().ToLowerInvariant();
            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

            // Unique checks
            var existingByEmail = await _accountRepository.GetAccountByEmailAsync(email);
            if (existingByEmail != null)
                throw new ArgumentException("Email đã tồn tại");

            // Phone uniqueness now validated against Users.PhoneNumber only

            // Generate secure random password (>=8, upper/lower/digit/special)
            var tempPassword = GenerateSecurePassword(12);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            // Create user
            var user = new User
            {
                Email = email,
                FullName = request.FullName.Trim(),
                PhoneNumber = normalizedPhone,
                PasswordHash = passwordHash,
                Role = "CUSTOMER",
                IsActive = true,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _accountRepository.CreateAccountAsync(user);

            // Create customer
            var customer = new Customer
            {
                UserId = user.UserId,
                IsGuest = false,
            };

            var created = await _customerRepository.CreateCustomerAsync(customer);

            // Send email with temporary password
            var subject = "Tài khoản khách hàng tại EV Service Center";
            var body = $"Xin chào {user.FullName},\n\nTài khoản của bạn đã được tạo.\nEmail: {user.Email}\nMật khẩu tạm: {tempPassword}\n\nVui lòng đăng nhập và đổi mật khẩu ngay trong phần tài khoản.\n\nTrân trọng.";
            await _emailService.SendEmailAsync(user.Email, subject, body);

            return MapToCustomerResponse(created);
        }

        private static string GenerateSecurePassword(int length)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "!@#$%^&*()-_=+";

            var rng = RandomNumberGenerator.Create();
            string Pick(string chars)
            {
                var b = new byte[1];
                rng.GetBytes(b);
                return chars[b[0] % chars.Length].ToString();
            }

            var sb = new StringBuilder();
            sb.Append(Pick(upper));
            sb.Append(Pick(lower));
            sb.Append(Pick(digits));
            sb.Append(Pick(specials));

            var all = upper + lower + digits + specials;
            while (sb.Length < Math.Max(8, length))
            {
                sb.Append(Pick(all));
            }

            return sb.ToString();
        }

        private CustomerResponse MapToCustomerResponse(Customer customer)
        {
            return new CustomerResponse
            {
                CustomerId = customer.CustomerId,
                UserId = customer.UserId,
                IsGuest = customer.IsGuest,
                UserFullName = customer.User?.FullName,
                UserEmail = customer.User?.Email,
                UserPhoneNumber = customer.User?.PhoneNumber,
                VehicleCount = customer.Vehicles?.Count ?? 0
            };
        }

        private async Task ValidateCreateCustomerRequestAsync(CreateCustomerRequest request)
        {
            var errors = new List<string>();

            // CustomerCode/NormalizedPhone removed from Customer; uniqueness handled on User

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateUpdateCustomerRequestAsync(UpdateCustomerRequest request, int customerId)
        {
            var errors = new List<string>();

            // CustomerCode/NormalizedPhone removed from Customer; uniqueness handled on User

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters and ensure it starts with 0
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return digits.StartsWith("0") ? digits : "0" + digits;
        }

        
    }
}
