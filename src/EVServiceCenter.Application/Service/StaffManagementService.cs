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

                // Generate staff code if not provided
                var staffCode = string.IsNullOrWhiteSpace(request.StaffCode) 
                    ? GenerateStaffCode() 
                    : request.StaffCode;

                // Create staff entity
                var staff = new Staff
                {
                    UserId = request.UserId,
                    CenterId = request.CenterId,
                    StaffCode = staffCode,
                    Position = request.Position,
                    HireDate = request.HireDate,
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

        public async Task<StaffListResponse> GetStaffByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string searchTerm = null, string position = null, bool? isActive = null)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");

                // Get all staff for the center
                var allStaff = await _staffRepository.GetStaffByCenterIdAsync(centerId);

                return await ProcessStaffListAsync(allStaff, pageNumber, pageSize, searchTerm, position, isActive);
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

        public async Task<StaffListResponse> GetAllStaffAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string position = null, bool? isActive = null)
        {
            try
            {
                // Get all staff
                var allStaff = await _staffRepository.GetAllStaffAsync();

                return await ProcessStaffListAsync(allStaff, pageNumber, pageSize, searchTerm, position, isActive);
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
                    s.StaffCode.ToLower().Contains(searchLower) ||
                    s.User.Email.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(position))
            {
                filteredStaff = filteredStaff.Where(s => s.Position.ToLower().Contains(position.ToLower()));
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
                StaffCode = s.StaffCode,
                Position = s.Position,
                HireDate = s.HireDate,
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

                // Validate staff code uniqueness if provided
                if (!string.IsNullOrWhiteSpace(request.StaffCode) && request.StaffCode != staff.StaffCode)
                {
                    if (!await _staffRepository.IsStaffCodeUniqueAsync(request.StaffCode, staffId))
                        throw new ArgumentException("Mã nhân viên đã tồn tại.");
                }

                // Update staff properties
                if (!string.IsNullOrWhiteSpace(request.StaffCode))
                    staff.StaffCode = request.StaffCode;

                if (!string.IsNullOrWhiteSpace(request.Position))
                    staff.Position = request.Position;

                if (request.HireDate.HasValue)
                    staff.HireDate = request.HireDate.Value;

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
                // Validate staff exists
                var staff = await _staffRepository.GetStaffByIdAsync(staffId);
                if (staff == null)
                    throw new ArgumentException("Nhân viên không tồn tại.");

                // Delete staff
                await _staffRepository.DeleteStaffAsync(staffId);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa nhân viên: {ex.Message}");
            }
        }

        public async Task<bool> IsStaffCodeUniqueAsync(string staffCode, int? excludeStaffId = null)
        {
            return await _staffRepository.IsStaffCodeUniqueAsync(staffCode, excludeStaffId);
        }

        #endregion

        #region Technician Management

        public async Task<TechnicianResponse> AddTechnicianToCenterAsync(AddTechnicianToCenterRequest request)
        {
            try
            {
                // Validate request
                await ValidateAddTechnicianRequestAsync(request);

                // Generate technician code if not provided
                var technicianCode = string.IsNullOrWhiteSpace(request.TechnicianCode) 
                    ? GenerateTechnicianCode() 
                    : request.TechnicianCode;

                // Create technician entity
                var technician = new Technician
                {
                    UserId = request.UserId,
                    CenterId = request.CenterId,
                    TechnicianCode = technicianCode,
                    Specialization = request.Specialization,
                    ExperienceYears = request.ExperienceYears,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Save technician
                var createdTechnician = await _technicianRepository.CreateTechnicianAsync(technician);

                return await MapToTechnicianResponseAsync(createdTechnician.TechnicianId);
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

        public async Task<TechnicianListResponse> GetTechniciansByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string searchTerm = null, string specialization = null, bool? isActive = null)
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
                        t.TechnicianCode.ToLower().Contains(searchLower) ||
                        t.User.Email.ToLower().Contains(searchLower) ||
                        t.Specialization.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrWhiteSpace(specialization))
                {
                    filteredTechnicians = filteredTechnicians.Where(t => t.Specialization.ToLower().Contains(specialization.ToLower()));
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
                    TechnicianCode = t.TechnicianCode,
                    Specialization = t.Specialization,
                    ExperienceYears = t.ExperienceYears,
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
                if (!string.IsNullOrWhiteSpace(request.TechnicianCode) && request.TechnicianCode != technician.TechnicianCode)
                {
                    if (!await _technicianRepository.IsTechnicianCodeUniqueAsync(request.TechnicianCode, technicianId))
                        throw new ArgumentException("Mã kỹ thuật viên đã tồn tại.");
                }

                // Update technician properties
                if (!string.IsNullOrWhiteSpace(request.TechnicianCode))
                    technician.TechnicianCode = request.TechnicianCode;

                if (!string.IsNullOrWhiteSpace(request.Specialization))
                    technician.Specialization = request.Specialization;

                if (request.ExperienceYears.HasValue)
                    technician.ExperienceYears = request.ExperienceYears.Value;

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
                // Validate technician exists
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                // Delete technician
                await _technicianRepository.DeleteTechnicianAsync(technicianId);

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<bool> IsTechnicianCodeUniqueAsync(string technicianCode, int? excludeTechnicianId = null)
        {
            return await _technicianRepository.IsTechnicianCodeUniqueAsync(technicianCode, excludeTechnicianId);
        }

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

        #endregion

        #region Private Helper Methods

        private async Task ValidateAddStaffRequestAsync(AddStaffToCenterRequest request)
        {
            var errors = new List<string>();

            // Validate user exists
            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user == null)
                errors.Add("Người dùng không tồn tại.");

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

            // Validate staff code uniqueness
            if (!string.IsNullOrWhiteSpace(request.StaffCode))
            {
                if (!await IsStaffCodeUniqueAsync(request.StaffCode))
                    errors.Add("Mã nhân viên đã tồn tại.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateAddTechnicianRequestAsync(AddTechnicianToCenterRequest request)
        {
            var errors = new List<string>();

            // Validate user exists
            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user == null)
                errors.Add("Người dùng không tồn tại.");

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

            // Validate technician code uniqueness
            if (!string.IsNullOrWhiteSpace(request.TechnicianCode))
            {
                if (!await IsTechnicianCodeUniqueAsync(request.TechnicianCode))
                    errors.Add("Mã kỹ thuật viên đã tồn tại.");
            }

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
                StaffCode = staff.StaffCode,
                Position = staff.Position,
                HireDate = staff.HireDate,
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
                TechnicianCode = technician.TechnicianCode,
                Specialization = technician.Specialization,
                ExperienceYears = technician.ExperienceYears,
                IsActive = technician.IsActive,
                CreatedAt = technician.CreatedAt
            };
        }

        private string GenerateStaffCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"ST{timestamp}{random}";
        }

        private string GenerateTechnicianCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"TC{timestamp}{random}";
        }

        #endregion
    }
}
