using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class StaffManagementService : IStaffManagementService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly IAuthRepository _userRepository;
        private readonly ICenterRepository _centerRepository;

        public StaffManagementService(
            IStaffRepository staffRepository,
            ITechnicianRepository technicianRepository,
            IAuthRepository userRepository,
            ICenterRepository centerRepository)
        {
            _staffRepository = staffRepository;
            _technicianRepository = technicianRepository;
            _userRepository = userRepository;
            _centerRepository = centerRepository;
        }

        #region Staff Management

        public async Task<StaffResponse> AddStaffToCenterAsync(AddStaffToCenterRequest request)
        {
            try
            {
                // Validate request
                await ValidateAddStaffRequestAsync(request);

                // Ràng buộc: 1 user chỉ có 1 staff active
                if (await _staffRepository.ExistsActiveByUserAsync(request.UserId))
                    throw new ArgumentException("Người dùng đã là nhân viên đang hoạt động ở một trung tâm khác.");

                // Create staff entity
                var staff = new Staff
                {
                    UserId = request.UserId,
                    CenterId = request.CenterId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Save staff
                var createdStaff = await _staffRepository.CreateStaffAsync(staff);

                return await MapToStaffResponseAsync(createdStaff.StaffId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm nhân viên vào trung tâm: {ex.Message}");
            }
        }

        public async Task<StaffResponse> GetStaffByIdAsync(int staffId)
        {
            try
            {
                var staff = await _staffRepository.GetStaffByIdAsync(staffId);
                if (staff == null)
                    throw new ArgumentException("Nhân viên không tồn tại.");

                return await MapToStaffResponseAsync(staffId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin nhân viên: {ex.Message}");
            }
        }

        public async Task<StaffResponse> GetStaffByUserIdAsync(int userId)
        {
            try
            {
                var staff = await _staffRepository.GetStaffByUserIdAsync(userId);
                if (staff == null)
                    throw new ArgumentException("Không tìm thấy thông tin staff cho user này.");

                return await MapToStaffResponseAsync(staff.StaffId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin staff theo userId: {ex.Message}");
            }
        }

        public async Task<StaffListResponse> GetStaffByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? position = null, bool? isActive = null)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");

                // Get all staff for the center
                var allStaff = await _staffRepository.GetStaffByCenterIdAsync(centerId);

                return await ProcessStaffListAsync(allStaff, pageNumber, pageSize, searchTerm ?? string.Empty, position ?? string.Empty, isActive);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách nhân viên: {ex.Message}");
            }
        }

        public async Task<StaffListResponse> GetAllStaffAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? position = null, bool? isActive = null)
        {
            try
            {
                // Get all staff
                var allStaff = await _staffRepository.GetAllStaffAsync();

                return await ProcessStaffListAsync(allStaff, pageNumber, pageSize, searchTerm ?? string.Empty, position ?? string.Empty, isActive);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách tất cả nhân viên: {ex.Message}");
            }
        }

        private Task<StaffListResponse> ProcessStaffListAsync(List<Staff> allStaff, int pageNumber, int pageSize, string searchTerm, string position, bool? isActive)
        {
            // Apply filters
            var filteredStaff = allStaff.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                filteredStaff = filteredStaff.Where(s =>
                    s.User.FullName.ToLower().Contains(searchLower) ||
                    s.User.Email.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(position))
            {
                // Position field removed from Staff; ignore filter
            }

            if (isActive.HasValue)
            {
                filteredStaff = filteredStaff.Where(s => s.IsActive == isActive.Value);
            }

            // Calculate pagination
            var totalCount = filteredStaff.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Apply pagination
            var pagedStaff = filteredStaff
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to response
            var staffResponses = pagedStaff.Select(s => new StaffResponse
            {
                StaffId = s.StaffId,
                UserId = s.UserId,
                UserFullName = s.User?.FullName ?? "N/A",
                UserEmail = s.User?.Email ?? "N/A",
                UserPhoneNumber = s.User?.PhoneNumber ?? "N/A",
                CenterId = s.CenterId,
                CenterName = s.Center?.CenterName ?? "N/A",
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            }).ToList();

            return Task.FromResult(new StaffListResponse
            {
                Staff = staffResponses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        public async Task<StaffResponse> UpdateStaffAsync(int staffId, UpdateStaffRequest request)
        {
            try
            {
                // Validate staff exists
                var staff = await _staffRepository.GetStaffByIdAsync(staffId);
                if (staff == null)
                    throw new ArgumentException("Nhân viên không tồn tại.");

                // Update staff properties
                // No StaffCode/Position/HireDate anymore

                if (request.IsActive.HasValue)
                    staff.IsActive = request.IsActive.Value;

                // Save changes
                await _staffRepository.UpdateStaffAsync(staff);

                return await MapToStaffResponseAsync(staffId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật nhân viên: {ex.Message}");
            }
        }

        public async Task<bool> RemoveStaffFromCenterAsync(int staffId)
        {
            try
            {
                var staff = await _staffRepository.GetStaffByIdAsync(staffId);
                if (staff == null)
                    throw new ArgumentException("Nhân viên không tồn tại.");

                // Validate: Không cho phép xóa khi center chỉ còn 1 staff (đảm bảo luôn có ít nhất 1 MANAGER và 1 STAFF)
                var centerStaff = await _staffRepository.GetStaffByCenterIdAsync(staff.CenterId);
                var activeStaff = centerStaff.Where(s => s.IsActive && s.User != null).ToList();

                // Đếm số lượng MANAGER và STAFF active
                var managerCount = activeStaff.Count(s => s.User.Role == "MANAGER");
                var staffCount = activeStaff.Count(s => s.User.Role == "STAFF");

                // Nếu đang xóa MANAGER và chỉ còn 1 MANAGER
                if (staff.User?.Role == "MANAGER" && managerCount == 1)
                {
                    throw new ArgumentException("Không thể xóa quản lý (MANAGER) cuối cùng. Mỗi trung tâm phải có ít nhất 1 quản lý.");
                }

                // Nếu đang xóa STAFF và chỉ còn 1 STAFF
                if (staff.User?.Role == "STAFF" && staffCount == 1)
                {
                    throw new ArgumentException("Không thể xóa nhân viên (STAFF) cuối cùng. Mỗi trung tâm phải có ít nhất 1 nhân viên.");
                }

                staff.IsActive = false;
                await _staffRepository.UpdateStaffAsync(staff);
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi vô hiệu hóa nhân viên: {ex.Message}");
            }
        }

        // IsStaffCodeUniqueAsync removed (no StaffCode)

        #endregion

        #region Technician Management

        public async Task<TechnicianResponse> AddTechnicianToCenterAsync(AddTechnicianToCenterRequest request)
        {
            try
            {
                // Chế độ duy nhất: dùng TechnicianId để gán vào center
                if (request.TechnicianId > 0)
                {
                    var existing = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId);
                    if (existing == null)
                        throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                    // Ràng buộc: 1 user chỉ có 1 technician active
                    // Nếu technician đã active ở center khác -> 409
                    if (existing.IsActive && existing.CenterId != request.CenterId)
                        throw new ArgumentException("Kỹ thuật viên đang hoạt động ở trung tâm khác.");

                    // Cập nhật center và các thuộc tính tùy chọn
                    existing.CenterId = request.CenterId;
                    // TechnicianCode removed
                    if (!string.IsNullOrWhiteSpace(request.Position)) existing.Position = request.Position;

                    await _technicianRepository.UpdateTechnicianAsync(existing);
                    return await MapToTechnicianResponseAsync(existing.TechnicianId);
                }
                throw new ArgumentException("TechnicianId phải > 0");
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm kỹ thuật viên vào trung tâm: {ex.Message}");
            }
        }

        public async Task<TechnicianResponse> GetTechnicianByIdAsync(int technicianId)
        {
            try
            {
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                return await MapToTechnicianResponseAsync(technicianId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<TechnicianListResponse> GetTechniciansByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? specialization = null, bool? isActive = null)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");

                // Get all technicians for the center
                var allTechnicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);

                // Apply filters
                var filteredTechnicians = allTechnicians.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    filteredTechnicians = filteredTechnicians.Where(t =>
                        t.User.FullName.ToLower().Contains(searchLower) ||
                        t.User.Email.ToLower().Contains(searchLower) ||
                        t.Position.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(specialization))
                {
                    filteredTechnicians = filteredTechnicians.Where(t => t.Position.ToLower().Contains(specialization.ToLower()));
                }

                if (isActive.HasValue)
                {
                    filteredTechnicians = filteredTechnicians.Where(t => t.IsActive == isActive.Value);
                }

                // Calculate pagination
                var totalCount = filteredTechnicians.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination
                var pagedTechnicians = filteredTechnicians
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to response
                var technicianResponses = pagedTechnicians.Select(t => new TechnicianResponse
                {
                    TechnicianId = t.TechnicianId,
                    UserId = t.UserId,
                    UserFullName = t.User?.FullName ?? "N/A",
                    UserEmail = t.User?.Email ?? "N/A",
                    UserPhoneNumber = t.User?.PhoneNumber ?? "N/A",
                    CenterId = t.CenterId,
                    CenterName = t.Center?.CenterName ?? "N/A",

                    Position = t.Position,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt
                }).ToList();

                return new TechnicianListResponse
                {
                    Technicians = technicianResponses,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<TechnicianResponse> UpdateTechnicianAsync(int technicianId, UpdateTechnicianRequest request)
        {
            try
            {
                // Validate technician exists
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                // Validate technician code uniqueness if provided
                // TechnicianCode uniqueness removed

                // Update technician properties
                // TechnicianCode removed

                if (!string.IsNullOrWhiteSpace(request.Position))
                    technician.Position = request.Position;

                if (request.IsActive.HasValue)
                    technician.IsActive = request.IsActive.Value;

                // Save changes
                await _technicianRepository.UpdateTechnicianAsync(technician);

                return await MapToTechnicianResponseAsync(technicianId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<bool> RemoveTechnicianFromCenterAsync(int technicianId)
        {
            try
            {
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                technician.IsActive = false;
                await _technicianRepository.UpdateTechnicianAsync(technician);
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi vô hiệu hóa kỹ thuật viên: {ex.Message}");
            }
        }

        // TechnicianCode uniqueness removed

        #endregion

        #region Validation Methods

        public async Task<bool> IsUserAlreadyStaffAsync(int userId)
        {
            return await _staffRepository.IsUserAlreadyStaffAsync(userId);
        }

        public async Task<bool> IsUserAlreadyTechnicianAsync(int userId)
        {
            return await _technicianRepository.IsUserAlreadyTechnicianAsync(userId);
        }

        public async Task<bool> CanUserBeAssignedToCenterAsync(int userId, int centerId)
        {
            // Check if user exists
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // Check if center exists
            var center = await _centerRepository.GetCenterByIdAsync(centerId);
            if (center == null)
                return false;

            // Check if user is already staff or technician
            var isAlreadyStaff = await IsUserAlreadyStaffAsync(userId);
            var isAlreadyTechnician = await IsUserAlreadyTechnicianAsync(userId);

            return !isAlreadyStaff && !isAlreadyTechnician;
        }

        public async Task<bool> CanUserBeAssignedAsStaffAsync(int userId, int centerId)
        {
            // Check if user exists
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // Check if user has STAFF role
            if (user.Role != "STAFF")
                return false;

            // Check if center exists
            var center = await _centerRepository.GetCenterByIdAsync(centerId);
            if (center == null)
                return false;

            // Check if user is already staff or technician
            var isAlreadyStaff = await IsUserAlreadyStaffAsync(userId);
            var isAlreadyTechnician = await IsUserAlreadyTechnicianAsync(userId);

            return !isAlreadyStaff && !isAlreadyTechnician;
        }

        public async Task<bool> CanUserBeAssignedAsTechnicianAsync(int userId, int centerId)
        {
            // Check if user exists
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // Check if user has TECHNICIAN role
            if (user.Role != "TECHNICIAN")
                return false;

            // Check if center exists
            var center = await _centerRepository.GetCenterByIdAsync(centerId);
            if (center == null)
                return false;

            // Check if user is already staff or technician
            var isAlreadyStaff = await IsUserAlreadyStaffAsync(userId);
            var isAlreadyTechnician = await IsUserAlreadyTechnicianAsync(userId);

            return !isAlreadyStaff && !isAlreadyTechnician;
        }

        #endregion

        #region Private Helper Methods

        private async Task ValidateAddStaffRequestAsync(AddStaffToCenterRequest request)
        {
            var errors = new List<string>();

            // Validate user exists
            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                errors.Add("Người dùng không tồn tại.");
            }
            else
            {
                // Validate user role must be MANAGER or STAFF
                if (user.Role != "MANAGER" && user.Role != "STAFF")
                {
                    errors.Add("Người dùng phải có vai trò MANAGER hoặc STAFF để được thêm vào trung tâm.");
                }
            }

            // Validate center exists
            var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
            if (center == null)
                errors.Add("Trung tâm không tồn tại.");

            // Validate user is not already staff
            if (await IsUserAlreadyStaffAsync(request.UserId))
                errors.Add("Người dùng đã là nhân viên của trung tâm khác.");

            // Validate user is not already technician
            if (await IsUserAlreadyTechnicianAsync(request.UserId))
                errors.Add("Người dùng đã là kỹ thuật viên của trung tâm khác.");

            // Validate maximum staff per center: 1 MANAGER and 1 STAFF
            if (user != null && center != null)
            {
                var existingStaff = await _staffRepository.GetStaffByCenterIdAsync(request.CenterId);
                var activeStaff = existingStaff.Where(s => s.IsActive && s.User != null).ToList();

                if (user.Role == "MANAGER")
                {
                    var existingManager = activeStaff.FirstOrDefault(s => s.User.Role == "MANAGER");
                    if (existingManager != null)
                    {
                        errors.Add("Trung tâm đã có một quản lý (MANAGER). Mỗi trung tâm chỉ được phép có tối đa 1 quản lý.");
                    }
                }
                else if (user.Role == "STAFF")
                {
                    var existingStaffRole = activeStaff.FirstOrDefault(s => s.User.Role == "STAFF");
                    if (existingStaffRole != null)
                    {
                        errors.Add("Trung tâm đã có một nhân viên (STAFF). Mỗi trung tâm chỉ được phép có tối đa 1 nhân viên.");
                    }
                }
            }

            // No StaffCode validation

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateAddTechnicianRequestAsync(AddTechnicianToCenterRequest request)
        {
            var errors = new List<string>();

            // Validate center exists
            var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
            if (center == null)
                errors.Add("Trung tâm không tồn tại.");

            // Validate technician exists
            var tech = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId);
            if (tech == null)
                errors.Add("Kỹ thuật viên không tồn tại.");

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task<StaffResponse> MapToStaffResponseAsync(int staffId)
        {
            var staff = await _staffRepository.GetStaffByIdAsync(staffId);
            if (staff == null)
                throw new ArgumentException("Nhân viên không tồn tại.");

            return new StaffResponse
            {
                StaffId = staff.StaffId,
                UserId = staff.UserId,
                UserFullName = staff.User?.FullName ?? "N/A",
                UserEmail = staff.User?.Email ?? "N/A",
                UserPhoneNumber = staff.User?.PhoneNumber ?? "N/A",
                CenterId = staff.CenterId,
                CenterName = staff.Center?.CenterName ?? "N/A",
                IsActive = staff.IsActive,
                CreatedAt = staff.CreatedAt
            };
        }

        private async Task<TechnicianResponse> MapToTechnicianResponseAsync(int technicianId)
        {
            var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null)
                throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                return new TechnicianResponse
            {
                TechnicianId = technician.TechnicianId,
                UserId = technician.UserId,
                UserFullName = technician.User?.FullName ?? "N/A",
                UserEmail = technician.User?.Email ?? "N/A",
                UserPhoneNumber = technician.User?.PhoneNumber ?? "N/A",
                CenterId = technician.CenterId,
                    CenterName = technician.Center?.CenterName ?? "N/A",
                    Position = technician.Position,
                IsActive = technician.IsActive,
                CreatedAt = technician.CreatedAt
            };
        }

        // GenerateStaffCode removed

        // GenerateTechnicianCode removed

        #endregion

        #region Employee Management (Staff + Technician)

        public async Task<EmployeeListResponse> GetUnassignedEmployeesAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                // Get staff and technicians with CenterId = 0 (unassigned)
                var allStaff = await _staffRepository.GetStaffByCenterIdAsync(0);
                var allTechnicians = await _technicianRepository.GetTechniciansByCenterIdAsync(0);

                // Combine and map to EmployeeResponse
                var allEmployees = new List<EmployeeResponse>();

                // Map Staff to EmployeeResponse
                foreach (var staff in allStaff)
                {
                    allEmployees.Add(new EmployeeResponse
                    {
                        Type = "STAFF",
                        StaffId = staff.StaffId,
                        TechnicianId = null,
                        UserId = staff.UserId,
                        FullName = staff.User?.FullName ?? "N/A",
                        Email = staff.User?.Email ?? "N/A",
                        PhoneNumber = staff.User?.PhoneNumber ?? "N/A",
                        Role = "STAFF",
                        IsActive = staff.IsActive,
                        CenterId = staff.CenterId,
                        CenterName = "Chưa gán trung tâm",
                        Position = null,
                        Rating = null,
                        CreatedAt = staff.CreatedAt
                    });
                }

                // Map Technician to EmployeeResponse
                foreach (var technician in allTechnicians)
                {
                    allEmployees.Add(new EmployeeResponse
                    {
                        Type = "TECHNICIAN",
                        StaffId = null,
                        TechnicianId = technician.TechnicianId,
                        UserId = technician.UserId,
                        FullName = technician.User?.FullName ?? "N/A",
                        Email = technician.User?.Email ?? "N/A",
                        PhoneNumber = technician.User?.PhoneNumber ?? "N/A",
                        Role = "TECHNICIAN",
                        IsActive = technician.IsActive,
                        CenterId = technician.CenterId,
                        CenterName = "Chưa gán trung tâm",
                        Position = technician.Position,
                        Rating = technician.Rating,
                        CreatedAt = technician.CreatedAt
                    });
                }

                // Apply filters
                var filteredEmployees = allEmployees.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    filteredEmployees = filteredEmployees.Where(e =>
                        e.FullName.ToLower().Contains(searchLower) ||
                        e.Email.ToLower().Contains(searchLower) ||
                        e.PhoneNumber.Contains(searchTerm));
                }

                if (isActive.HasValue)
                {
                    filteredEmployees = filteredEmployees.Where(e => e.IsActive == isActive.Value);
                }

                // Apply pagination
                var totalCount = filteredEmployees.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var pagedEmployees = filteredEmployees
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new EmployeeListResponse
                {
                    Employees = pagedEmployees,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách nhân viên chưa có centerId: {ex.Message}");
            }
        }

        public async Task<EmployeeListResponse> GetAvailableUsersForEmployeeAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                // Get all users with role STAFF or TECHNICIAN
                var allUsers = await _userRepository.GetAllUsersAsync();
                var staffTechnicianUsers = allUsers.Where(u => u.Role == "STAFF" || u.Role == "TECHNICIAN").ToList();

                // Get existing staff and technician user IDs
                var existingStaffUserIds = (await _staffRepository.GetAllStaffAsync()).Select(s => s.UserId).ToHashSet();
                var existingTechnicianUserIds = (await _technicianRepository.GetAllTechniciansAsync()).Select(t => t.UserId).ToHashSet();

                // Filter users who don't have staff or technician records
                var availableUsers = staffTechnicianUsers.Where(u =>
                    !existingStaffUserIds.Contains(u.UserId) &&
                    !existingTechnicianUserIds.Contains(u.UserId)).ToList();

                // Map to EmployeeResponse
                var allEmployees = new List<EmployeeResponse>();
                foreach (var user in availableUsers)
                {
                    allEmployees.Add(new EmployeeResponse
                    {
                        Type = user.Role ?? "N/A",
                        StaffId = null,
                        TechnicianId = null,
                        UserId = user.UserId,
                        FullName = user.FullName ?? "N/A",
                        Email = user.Email ?? "N/A",
                        PhoneNumber = user.PhoneNumber ?? "N/A",
                        Role = user.Role ?? "N/A",
                        IsActive = user.IsActive,
                        CenterId = 0,
                        CenterName = "Chưa tạo bản ghi nhân viên",
                        Position = null,
                        Rating = null,
                        CreatedAt = user.CreatedAt
                    });
                }

                // Apply filters
                var filteredEmployees = allEmployees.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    filteredEmployees = filteredEmployees.Where(e =>
                        e.FullName.ToLower().Contains(searchLower) ||
                        e.Email.ToLower().Contains(searchLower) ||
                        e.PhoneNumber.Contains(searchTerm));
                }

                if (isActive.HasValue)
                {
                    filteredEmployees = filteredEmployees.Where(e => e.IsActive == isActive.Value);
                }

                // Apply pagination
                var totalCount = filteredEmployees.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var pagedEmployees = filteredEmployees
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new EmployeeListResponse
                {
                    Employees = pagedEmployees,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách user có thể làm nhân viên: {ex.Message}");
            }
        }

        public async Task<EmployeeListResponse> GetCenterEmployeesAsync(int? centerId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                // Validate center exists if centerId is provided
                if (centerId.HasValue)
                {
                    var center = await _centerRepository.GetCenterByIdAsync(centerId.Value);
                    if (center == null)
                        throw new ArgumentException("Trung tâm không tồn tại.");
                }

                // Get staff and technicians
                var allStaff = centerId.HasValue
                    ? await _staffRepository.GetStaffByCenterIdAsync(centerId.Value)
                    : await _staffRepository.GetStaffByCenterIdAsync(null); // Get unassigned staff

                var allTechnicians = centerId.HasValue
                    ? await _technicianRepository.GetTechniciansByCenterIdAsync(centerId.Value)
                    : await _technicianRepository.GetTechniciansByCenterIdAsync(null); // Get unassigned technicians

                // Combine and map to EmployeeResponse
                var allEmployees = new List<EmployeeResponse>();

                // Map Staff to EmployeeResponse
                foreach (var staff in allStaff)
                {
                    allEmployees.Add(new EmployeeResponse
                    {
                        Type = "STAFF",
                        StaffId = staff.StaffId,
                        TechnicianId = null,
                        UserId = staff.UserId,
                        FullName = staff.User?.FullName ?? "N/A",
                        Email = staff.User?.Email ?? "N/A",
                        PhoneNumber = staff.User?.PhoneNumber ?? "N/A",
                        Role = "STAFF",
                        IsActive = staff.IsActive,
                        CenterId = staff.CenterId,
                        CenterName = staff.Center?.CenterName ?? "N/A",
                        Position = null,
                        Rating = null,
                        CreatedAt = staff.CreatedAt
                    });
                }

                // Map Technician to EmployeeResponse
                foreach (var technician in allTechnicians)
                {
                    allEmployees.Add(new EmployeeResponse
                    {
                        Type = "TECHNICIAN",
                        StaffId = null,
                        TechnicianId = technician.TechnicianId,
                        UserId = technician.UserId,
                        FullName = technician.User?.FullName ?? "N/A",
                        Email = technician.User?.Email ?? "N/A",
                        PhoneNumber = technician.User?.PhoneNumber ?? "N/A",
                        Role = "TECHNICIAN",
                        IsActive = technician.IsActive,
                        CenterId = technician.CenterId,
                        CenterName = technician.Center?.CenterName ?? "N/A",
                        Position = technician.Position,
                        Rating = technician.Rating,
                        CreatedAt = technician.CreatedAt
                    });
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    allEmployees = allEmployees.Where(e =>
                        e.FullName.ToLower().Contains(searchLower) ||
                        e.Email.ToLower().Contains(searchLower) ||
                        e.PhoneNumber.ToLower().Contains(searchLower)
                    ).ToList();
                }

                // Apply isActive filter
                if (isActive.HasValue)
                {
                    allEmployees = allEmployees.Where(e => e.IsActive == isActive.Value).ToList();
                }

                // Calculate pagination
                var totalCount = allEmployees.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination
                var pagedEmployees = allEmployees
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new EmployeeListResponse
                {
                    Employees = pagedEmployees,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách nhân viên: {ex.Message}");
            }
        }

        #endregion

        #region Assignment APIs

        public async Task<List<object>> AssignEmployeesToCenterAsync(List<int> userIds, int centerId)
        {
            var results = new List<object>();
            var errors = new List<string>();

            // Kiểm tra center tồn tại
            var center = await _centerRepository.GetCenterByIdAsync(centerId);
            if (center == null)
                throw new ArgumentException("Trung tâm không tồn tại.");

            // Lấy danh sách staff hiện tại của center để kiểm tra giới hạn
            var existingStaff = await _staffRepository.GetStaffByCenterIdAsync(centerId);
            var activeStaff = existingStaff.Where(s => s.IsActive && s.User != null).ToList();
            var currentManagerCount = activeStaff.Count(s => s.User.Role == "MANAGER");
            var currentStaffCount = activeStaff.Count(s => s.User.Role == "STAFF");

            foreach (var userId in userIds)
            {
                try
                {
                    // Lấy user
                    var user = await _userRepository.GetUserByIdAsync(userId);
                    if (user == null)
                    {
                        errors.Add($"User ID {userId} không tồn tại.");
                        continue;
                    }

                    // Kiểm tra role phải là MANAGER, STAFF hoặc TECHNICIAN
                    if (user.Role != "MANAGER" && user.Role != "STAFF" && user.Role != "TECHNICIAN")
                    {
                        errors.Add($"User ID {userId} không phải là MANAGER, STAFF hoặc TECHNICIAN.");
                        continue;
                    }

                    // Validate giới hạn MANAGER và STAFF
                    if (user.Role == "MANAGER")
                    {
                        // Kiểm tra center đã có MANAGER chưa
                        if (currentManagerCount >= 1)
                        {
                            errors.Add($"Trung tâm đã có một quản lý (MANAGER). Mỗi trung tâm chỉ được phép có tối đa 1 quản lý.");
                            continue;
                        }
                    }
                    else if (user.Role == "STAFF")
                    {
                        // Kiểm tra center đã có STAFF chưa
                        if (currentStaffCount >= 1)
                        {
                            errors.Add($"Trung tâm đã có một nhân viên (STAFF). Mỗi trung tâm chỉ được phép có tối đa 1 nhân viên.");
                            continue;
                        }
                    }

                    // Xử lý theo role
                    if (user.Role == "MANAGER" || user.Role == "STAFF")
                    {
                        var staff = await _staffRepository.GetStaffByUserIdAsync(userId);

                        if (staff == null)
                        {
                            // Tạo mới staff
                            staff = new Staff
                            {
                                UserId = userId,
                                CenterId = centerId,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _staffRepository.CreateStaffAsync(staff);

                            // Cập nhật counter sau khi thêm thành công
                            if (user.Role == "MANAGER")
                                currentManagerCount++;
                            else if (user.Role == "STAFF")
                                currentStaffCount++;
                        }
                        else
                        {
                            // Kiểm tra staff đã có center chưa (chỉ cho phép nếu CenterId = 0 hoặc bằng centerId hiện tại)
                            if (staff.CenterId != 0 && staff.CenterId != centerId)
                            {
                                errors.Add($"Nhân viên (User ID {userId}) đã được gán vào trung tâm khác.");
                                continue;
                            }

                            // Lưu trạng thái trước khi update để kiểm tra
                            var wasActive = staff.IsActive;
                            var oldCenterId = staff.CenterId;

                            // Cập nhật center
                            staff.CenterId = centerId;
                            staff.IsActive = true;

                            await _staffRepository.UpdateStaffAsync(staff);

                            // Cập nhật counter sau khi cập nhật thành công (chỉ tăng nếu staff chưa active hoặc chưa ở center này)
                            if ((!wasActive || oldCenterId != centerId) && staff.IsActive && staff.CenterId == centerId)
                            {
                                if (user.Role == "MANAGER")
                                    currentManagerCount++;
                                else if (user.Role == "STAFF")
                                    currentStaffCount++;
                            }
                        }

                        results.Add(await MapToStaffResponseAsync(staff.StaffId));
                    }
                    else // TECHNICIAN
                    {
                        var technician = await _technicianRepository.GetTechnicianByUserIdAsync(userId);

                        if (technician == null)
                        {
                            // Tạo mới technician
                            technician = new Technician
                            {
                                UserId = userId,
                                CenterId = centerId,
                                Position = "Kỹ thuật viên", // Set default position
                                Rating = 0, // Set default rating
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _technicianRepository.CreateTechnicianAsync(technician);
                        }
                        else
                        {
                            // Kiểm tra technician đã có center chưa (chỉ cho phép nếu CenterId = 0 hoặc bằng centerId hiện tại)
                            if (technician.CenterId != 0 && technician.CenterId != centerId)
                            {
                                errors.Add($"Kỹ thuật viên (User ID {userId}) đã được gán vào trung tâm khác.");
                                continue;
                            }

                            // Cập nhật center
                            technician.CenterId = centerId;
                            technician.IsActive = true;

                            await _technicianRepository.UpdateTechnicianAsync(technician);
                        }

                        results.Add(await MapToTechnicianResponseAsync(technician.TechnicianId));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi với User ID {userId}: {ex.Message}");
                }
            }

            if (errors.Any() && results.Count == 0)
                throw new ArgumentException(string.Join("; ", errors));

            return results;
        }

        #endregion
    }
}
