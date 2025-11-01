using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using BCrypt.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace EVServiceCenter.Application.Service
{
    public class UserService : IUserService
    {
        private readonly IAuthRepository _authRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;

        public UserService(IAuthRepository authRepository, IAccountRepository accountRepository, ICustomerRepository customerRepository, IStaffRepository staffRepository, ITechnicianRepository technicianRepository, IEmailService emailService, IOtpService otpService)
        {
            _authRepository = authRepository;
            _accountRepository = accountRepository;
            _customerRepository = customerRepository;
            _staffRepository = staffRepository;
            _technicianRepository = technicianRepository;
            _emailService = emailService;
            _otpService = otpService;
        }

        public async Task<UserListResponse> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? role = null)
        {
            try
            {
                var users = await _authRepository.GetAllUsersAsync();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    users = users.Where(u => 
                        u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (u.PhoneNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(role))
                {
                    users = users.Where(u => (u.Role ?? string.Empty).Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Calculate pagination
                var totalCount = users.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var pagedUsers = users
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var userResponses = pagedUsers.Select(MapToUserResponse).ToList();

                return new UserListResponse
                {
                    Users = userResponses,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách người dùng: {ex.Message}");
            }
        }

        public async Task<UserResponse> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                return MapToUserResponse(user);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin người dùng: {ex.Message}");
            }
        }

        public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreateUserRequestAsync(request);

                // Create user entity
                var user = new User
                {
                    Email = request.Email.ToLower().Trim(),
                    FullName = request.FullName.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateTempPassword(out var tempPassword)),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Address = !string.IsNullOrWhiteSpace(request.Address) ? request.Address.Trim() : null,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Role = request.Role?.Trim().ToUpper(),
                    IsActive = true, // Mặc định là active
                    EmailVerified = false, // Luôn yêu cầu xác minh email
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save user
                await _authRepository.RegisterAsync(user);

                // Tạo record tương ứng dựa trên role
                if (user.Role == "CUSTOMER")
                {
                    var customer = new Customer
                    {
                        UserId = user.UserId,
                        IsGuest = false
                    };

                    await _customerRepository.CreateCustomerAsync(customer);
                }

                // Gửi email chào mừng kèm mật khẩu tạm (không gửi OTP)
                await _emailService.SendWelcomeCustomerWithPasswordAsync(user.Email, user.FullName, tempPassword);

                return MapToUserResponse(user);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Parse specific database errors
                var errorMessage = "Lỗi cơ sở dữ liệu";
                
                if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    switch (sqlEx.Number)
                    {
                        case 2628: // String or binary data would be truncated
                            errorMessage = "Dữ liệu quá dài cho một số trường. Vui lòng kiểm tra lại thông tin.";
                            break;
                        case 2627: // Violation of UNIQUE KEY constraint
                            errorMessage = "Thông tin này đã tồn tại trong hệ thống.";
                            break;
                        case 547: // Foreign key constraint violation
                            errorMessage = "Dữ liệu tham chiếu không hợp lệ.";
                            break;
                        default:
                            errorMessage = $"Lỗi cơ sở dữ liệu: {sqlEx.Message}";
                            break;
                    }
                }
                
                throw new ArgumentException(errorMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo người dùng: {ex.Message}");
            }
        }

        public async Task<UserResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            try
            {
                // Validate request
                await ValidateUpdateUserRequestAsync(userId, request);

                // Get existing user
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                // Update user properties
                user.FullName = request.FullName.Trim();
                user.PhoneNumber = request.PhoneNumber.Trim();
                user.DateOfBirth = request.DateOfBirth;
                user.Gender = request.Gender;
                user.Address = !string.IsNullOrWhiteSpace(request.Address) ? request.Address.Trim() : null;
                user.Role = request.Role;
                user.IsActive = request.IsActive;
                user.EmailVerified = request.EmailVerified;
                user.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _authRepository.UpdateUserAsync(user);

                return MapToUserResponse(user);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật người dùng: {ex.Message}");
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                // Soft delete by deactivating
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa người dùng: {ex.Message}");
            }
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            try
            {
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kích hoạt người dùng: {ex.Message}");
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new ArgumentException("Người dùng không tồn tại.");

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _authRepository.UpdateUserAsync(user);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi vô hiệu hóa người dùng: {ex.Message}");
            }
        }

        private async Task ValidateCreateUserRequestAsync(CreateUserRequest request)
        {
            var errors = new List<string>();

            // Check email format
            if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
            {
                errors.Add("Email không đúng định dạng");
            }

            // Password không yêu cầu khi tạo (dùng mật khẩu tạm gửi email)

            // Check phone number format
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                errors.Add("Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.");
            }

            // Check age
            if (!IsValidAge(request.DateOfBirth))
            {
                errors.Add("Phải đủ 16 tuổi trở lên.");
            }

            // Check gender
            if (!IsValidGender(request.Gender))
            {
                errors.Add("Giới tính phải là MALE hoặc FEMALE.");
            }

            // Validate role cho ADMIN: cho phép ADMIN/STAFF/TECHNICIAN/CUSTOMER
            if (!IsValidRole(request.Role))
            {
                errors.Add("Vai trò không hợp lệ. Vai trò phải là ADMIN, STAFF, TECHNICIAN hoặc CUSTOMER.");
            }

            // EmailVerified: cho phép ADMIN đánh dấu đã xác thực để bỏ qua bước OTP

            // Check email uniqueness
            var existingUserByEmail = await _accountRepository.GetAccountByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                errors.Add("Email này đã được sử dụng. Vui lòng sử dụng email khác.");
            }

            // Check phone uniqueness
            var existingUserByPhone = await _accountRepository.GetAccountByPhoneNumberAsync(request.PhoneNumber);
            if (existingUserByPhone != null)
            {
                errors.Add("Số điện thoại này đã được sử dụng. Vui lòng sử dụng số điện thoại khác.");
            }

            if (errors.Any())
            {
                throw new ArgumentException(string.Join(" ", errors));
            }
        }

        private async Task ValidateUpdateUserRequestAsync(int userId, UpdateUserRequest request)
        {
            var errors = new List<string>();

            // Check phone number format
            if (!IsValidPhoneNumber(request.PhoneNumber))
            {
                errors.Add("Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.");
            }

            // Check age
            if (!IsValidAge(request.DateOfBirth))
            {
                errors.Add("Phải đủ 16 tuổi trở lên.");
            }

            // Check gender
            if (!IsValidGender(request.Gender))
            {
                errors.Add("Giới tính phải là MALE hoặc FEMALE.");
            }

            // Check role
            if (!IsValidRole(request.Role))
            {
                errors.Add("Vai trò phải là ADMIN, MANAGER, STAFF, TECHNICIAN hoặc CUSTOMER.");
            }

            // Check phone uniqueness (excluding current user)
            var existingUserByPhone = await _accountRepository.GetAccountByPhoneNumberAsync(request.PhoneNumber);
            if (existingUserByPhone != null && existingUserByPhone.UserId != userId)
            {
                errors.Add("Số điện thoại này đã được sử dụng. Vui lòng sử dụng số điện thoại khác.");
            }

            if (errors.Any())
            {
                throw new ArgumentException(string.Join(" ", errors));
            }
        }

        // Cho phép tất cả domain hợp lệ theo chuẩn EmailAddressAttribute
        private bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email);

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

            phoneNumber = phoneNumber.Replace(" ", "");
            var phoneRegex = new Regex(@"^0\d{9}$");
            return phoneRegex.IsMatch(phoneNumber);
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

        private bool IsValidRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            var validRoles = new[] { "ADMIN", "MANAGER", "STAFF", "TECHNICIAN", "CUSTOMER" };
            return validRoles.Contains(role, StringComparer.Ordinal);
        }

        private UserResponse MapToUserResponse(User user)
        {
            return new UserResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address ?? string.Empty,
                Gender = user.Gender ?? string.Empty,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                Role = user.Role ?? string.Empty,
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private string GenerateCustomerCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"CUS{timestamp}{random}";
        }

        private string GenerateStaffCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            var random = new Random().Next(10, 99);
            return $"ST{timestamp}{random}";
        }
        private string GenerateTempPassword(out string plain)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789@$!%*?&";
            var rnd = new Random();
            plain = new string(Enumerable.Range(0, 12).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
            return plain;
        }

        private string GenerateTempPasswordEmailBody(string fullName, string tempPwd)
        {
            return $@"<!DOCTYPE html><html><body style='font-family:Arial,sans-serif'>
<h2>EV Service Center - Tài khoản được tạo</h2>
<p>Xin chào {fullName},</p>
<p>Tài khoản của bạn đã được tạo thành công. Mật khẩu tạm thời của bạn là: <strong>{tempPwd}</strong></p>
<p>Vui lòng đăng nhập và đổi mật khẩu ngay để đảm bảo an toàn.</p>
<p>Trân trọng,</p>
<p>EV Service Center</p>
</body></html>";
        }

        // GenerateTechnicianCode removed

        /// <summary>
        /// Cập nhật trạng thái người dùng (activate/deactivate)
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <param name="isActive">Trạng thái mới (true = activate, false = deactivate)</param>
        /// <returns>Kết quả cập nhật</returns>
        public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                // Lấy thông tin user
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("Không tìm thấy người dùng với ID này.");
                }

                // Cập nhật trạng thái
                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                // Lưu thay đổi
                await _authRepository.UpdateUserAsync(user);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật trạng thái người dùng: {ex.Message}");
            }
        }

        /// <summary>
        /// Gán vai trò cho người dùng (chỉ Admin)
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <param name="role">Vai trò mới</param>
        /// <returns>Kết quả gán vai trò</returns>
        public async Task<bool> AssignUserRoleAsync(int userId, string role)
        {
            try
            {
                // Validate role
                if (!IsValidRole(role))
                {
                    throw new ArgumentException("Vai trò không hợp lệ. Vai trò phải là ADMIN, STAFF, TECHNICIAN hoặc CUSTOMER.");
                }

                // Lấy thông tin user
                var user = await _authRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("Không tìm thấy người dùng với ID này.");
                }

                // Cập nhật role
                user.Role = role;
                user.UpdatedAt = DateTime.UtcNow;

                // Lưu thay đổi
                await _authRepository.UpdateUserAsync(user);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi gán vai trò cho người dùng: {ex.Message}");
            }
        }

        public async Task<int> GetUsersCountAsync(string? searchTerm = null, string? role = null, bool? isActive = null, bool? emailVerified = null, DateTime? createdFrom = null, DateTime? createdTo = null)
        {
            var users = await _authRepository.GetAllUsersAsync();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.PhoneNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }
            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => (u.Role ?? string.Empty).Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (isActive.HasValue)
            {
                users = users.Where(u => u.IsActive == isActive.Value).ToList();
            }
            if (emailVerified.HasValue)
            {
                users = users.Where(u => u.EmailVerified == emailVerified.Value).ToList();
            }
            if (createdFrom.HasValue)
            {
                users = users.Where(u => u.CreatedAt >= createdFrom.Value).ToList();
            }
            if (createdTo.HasValue)
            {
                users = users.Where(u => u.CreatedAt <= createdTo.Value).ToList();
            }
            return users.Count;
        }

        public async Task<IList<UserResponse>> GetUsersForExportAsync(string? searchTerm = null, string? role = null, int maxRecords = 100000, bool? isActive = null, bool? emailVerified = null, DateTime? createdFrom = null, DateTime? createdTo = null)
        {
            var users = await _authRepository.GetAllUsersAsync();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.PhoneNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }
            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => (u.Role ?? string.Empty).Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (isActive.HasValue)
            {
                users = users.Where(u => u.IsActive == isActive.Value).ToList();
            }
            if (emailVerified.HasValue)
            {
                users = users.Where(u => u.EmailVerified == emailVerified.Value).ToList();
            }
            if (createdFrom.HasValue)
            {
                users = users.Where(u => u.CreatedAt >= createdFrom.Value).ToList();
            }
            if (createdTo.HasValue)
            {
                users = users.Where(u => u.CreatedAt <= createdTo.Value).ToList();
            }
            var limited = users
                .OrderBy(u => u.UserId)
                .Take(maxRecords)
                .Select(MapToUserResponse)
                .ToList();
            return limited;
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
    }
}
