using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Chỉ Admin mới được quản lý staff
    public class StaffManagementController : ControllerBase
    {
        private readonly IStaffManagementService _staffManagementService;
        private readonly EVServiceCenter.Domain.Interfaces.ITechnicianRepository _technicianRepository;

        public StaffManagementController(IStaffManagementService staffManagementService, EVServiceCenter.Domain.Interfaces.ITechnicianRepository technicianRepository)
        {
            _staffManagementService = staffManagementService;
            _technicianRepository = technicianRepository;
        }

        #region Staff Management APIs

        /// <summary>
        /// Thêm nhân viên vào trung tâm
        /// </summary>
        /// <param name="request">Thông tin nhân viên</param>
        /// <returns>Thông tin nhân viên đã thêm</returns>
        [HttpPost("staff")]
        public async Task<IActionResult> AddStaffToCenter([FromBody] AddStaffToCenterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                try
                {
                    var staff = await _staffManagementService.AddStaffToCenterAsync(request);
                
                    return CreatedAtAction(nameof(GetStaffById), new { id = staff.StaffId }, new {
                        success = true,
                        message = "Thêm nhân viên vào trung tâm thành công",
                        data = staff
                    });
                }
                catch (System.Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException || ex is System.Data.Common.DbException)
                {
                    return Conflict(new { success = false, message = "User đã có bản ghi Staff đang hoạt động ở trung tâm khác." });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Lấy thông tin nhân viên theo ID
        /// </summary>
        /// <param name="id">ID nhân viên</param>
        /// <returns>Thông tin nhân viên</returns>
        [HttpGet("staff/{id}")]
        public async Task<IActionResult> GetStaffById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID nhân viên không hợp lệ" });

                var staff = await _staffManagementService.GetStaffByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin nhân viên thành công",
                    data = staff
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Tạo staff từ userId và gán vào center (đồng bộ hồ sơ Staff)
        /// </summary>
        [HttpPost("staff/from-user")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateStaffFromUser([FromBody] CreateStaffFromUserRequest request)
        {
            if (request == null || request.UserId <= 0 || request.CenterId <= 0)
                return BadRequest(new { success = false, message = "userId và centerId bắt buộc" });

            try
            {
                // Kiểm tra user có tồn tại và có role STAFF không
                var canAssign = await _staffManagementService.CanUserBeAssignedAsStaffAsync(request.UserId, request.CenterId);
                if (!canAssign)
                    return BadRequest(new { success = false, message = "User không tồn tại hoặc không có quyền làm nhân viên. Chỉ user có role STAFF mới được phép." });

                var staff = await _staffManagementService.AddStaffToCenterAsync(new AddStaffToCenterRequest
                {
                    UserId = request.UserId,
                    CenterId = request.CenterId
                });
                return CreatedAtAction(nameof(GetStaffById), new { id = staff.StaffId }, new { success = true, data = staff });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { success = false, message = "Vi phạm ràng buộc: user chỉ được có 1 staff active." });
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class CreateStaffFromUserRequest { public int UserId { get; set; } public int CenterId { get; set; } }

        /// <summary>
        /// Lấy danh sách nhân viên theo trung tâm hoặc tất cả nhân viên
        /// </summary>
        /// <param name="centerId">ID trung tâm (optional - nếu không có sẽ lấy tất cả)</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (tên, mã, email)</param>
        /// <param name="position">Lọc theo vị trí</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
        /// <returns>Danh sách nhân viên</returns>
        [HttpGet("staff")]
        public async Task<IActionResult> GetStaffByCenter(
            [FromQuery] int? centerId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? position = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (centerId.HasValue && centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // Nếu không có centerId, lấy tất cả staff
                if (!centerId.HasValue)
                {
                    var allStaff = await _staffManagementService.GetAllStaffAsync(pageNumber, pageSize, searchTerm, position, isActive);
                    return Ok(new { 
                        success = true, 
                        message = "Lấy danh sách tất cả nhân viên thành công",
                        data = allStaff
                    });
                }

                var result = await _staffManagementService.GetStaffByCenterAsync(centerId.Value, pageNumber, pageSize, searchTerm, position, isActive);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách nhân viên thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="id">ID nhân viên</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin nhân viên đã cập nhật</returns>
        [HttpPut("staff/{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID nhân viên không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var staff = await _staffManagementService.UpdateStaffAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật thông tin nhân viên thành công",
                    data = staff
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        // Deactivate staff (soft delete)
        [HttpDelete("staff/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeactivateStaff(int id)
        {
            try
            {
                var ok = await _staffManagementService.RemoveStaffFromCenterAsync(id);
                return Ok(new { success = ok, message = ok ? "Đã vô hiệu hóa nhân viên" : "Không thể vô hiệu hóa" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Technician Management APIs

        /// <summary>
        /// Thêm kỹ thuật viên vào trung tâm
        /// </summary>
        /// <param name="request">Thông tin kỹ thuật viên</param>
        /// <returns>Thông tin kỹ thuật viên đã thêm</returns>
        [HttpPost("technician")]
        public async Task<IActionResult> AddTechnicianToCenter([FromBody] AddTechnicianToCenterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                try
                {
                    var technician = await _staffManagementService.AddTechnicianToCenterAsync(request);
                    return CreatedAtAction(nameof(GetTechnicianById), new { id = technician.TechnicianId }, new {
                        success = true,
                        message = "Thêm kỹ thuật viên vào trung tâm thành công",
                        data = technician
                    });
                }
                catch (System.Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException || ex is System.Data.Common.DbException)
                {
                    return Conflict(new { success = false, message = "User đã có bản ghi Technician đang hoạt động ở trung tâm khác." });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Lấy thông tin kỹ thuật viên theo ID
        /// </summary>
        /// <param name="id">ID kỹ thuật viên</param>
        /// <returns>Thông tin kỹ thuật viên</returns>
        [HttpGet("technician/{id}")]
        public async Task<IActionResult> GetTechnicianById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                var technician = await _staffManagementService.GetTechnicianByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin kỹ thuật viên thành công",
                    data = technician
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Tạo kỹ thuật viên từ userId và gán vào center (đồng bộ hồ sơ Technician)
        /// </summary>
        [HttpPost("technician/from-user")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateTechnicianFromUser([FromBody] CreateTechnicianFromUserRequest request)
        {
            if (request == null || request.UserId <= 0 || request.CenterId <= 0)
                return BadRequest(new { success = false, message = "userId và centerId bắt buộc" });

            try
            {
                // Kiểm tra user có tồn tại và có role TECHNICIAN không
                var canAssign = await _staffManagementService.CanUserBeAssignedAsTechnicianAsync(request.UserId, request.CenterId);
                if (!canAssign)
                    return BadRequest(new { success = false, message = "User không tồn tại hoặc không có quyền làm kỹ thuật viên. Chỉ user có role TECHNICIAN mới được phép." });

                // Nếu user đã là technician active ở center khác -> 409
                var exists = await _staffManagementService.IsUserAlreadyTechnicianAsync(request.UserId);
                if (exists)
                    return Conflict(new { success = false, message = "User đã có hồ sơ kỹ thuật viên." });

                // Tạo technician entity trực tiếp qua repository/service hiện có
                var tech = await _technicianRepository.CreateTechnicianAsync(new EVServiceCenter.Domain.Entities.Technician
                {
                    UserId = request.UserId,
                    CenterId = request.CenterId,
                    Position = string.IsNullOrWhiteSpace(request.Position) ? "GENERAL" : request.Position,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                return CreatedAtAction(nameof(GetTechnicianById), new { id = tech.TechnicianId }, new { success = true, data = new { tech.TechnicianId, tech.UserId, tech.CenterId, tech.Position, tech.IsActive } });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Conflict(new { success = false, message = "Vi phạm ràng buộc: user chỉ được có 1 technician active." });
            }
        }

        public class CreateTechnicianFromUserRequest { public int UserId { get; set; } public int CenterId { get; set; } public string Position { get; set; } = string.Empty; }

        /// <summary>
        /// Lấy danh sách kỹ thuật viên theo trung tâm
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (tên, mã, email, chuyên môn)</param>
        /// <param name="specialization">Lọc theo chuyên môn</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
        /// <returns>Danh sách kỹ thuật viên</returns>
        [HttpGet("technician")]
        public async Task<IActionResult> GetTechniciansByCenter(
            [FromQuery] int centerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? specialization = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _staffManagementService.GetTechniciansByCenterAsync(centerId, pageNumber, pageSize, searchTerm, specialization, isActive);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách kỹ thuật viên thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Cập nhật thông tin kỹ thuật viên
        /// </summary>
        /// <param name="id">ID kỹ thuật viên</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin kỹ thuật viên đã cập nhật</returns>
        [HttpPut("technician/{id}")]
        public async Task<IActionResult> UpdateTechnician(int id, [FromBody] UpdateTechnicianRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var technician = await _staffManagementService.UpdateTechnicianAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật thông tin kỹ thuật viên thành công",
                    data = technician
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        // Deactivate technician (soft delete)
        [HttpDelete("technician/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeactivateTechnician(int id)
        {
            try
            {
                var ok = await _staffManagementService.RemoveTechnicianFromCenterAsync(id);
                return Ok(new { success = ok, message = ok ? "Đã vô hiệu hóa kỹ thuật viên" : "Không thể vô hiệu hóa" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Current User APIs

        /// <summary>
        /// Lấy thông tin staff hiện tại (dựa trên JWT token)
        /// </summary>
        /// <returns>Thông tin staff hiện tại</returns>
        [HttpGet("staff/current")]
        [Authorize] // Tạm thời bỏ policy để test
        public async Task<IActionResult> GetCurrentStaff()
        {
            try
            {
                // Debug: Log tất cả claims để xem có gì
                var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst("sub") ?? User.FindFirst("nameid");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { 
                        success = false, 
                        message = "Không thể xác định người dùng",
                        debug = new { 
                            claims = allClaims,
                            userIdClaim = userIdClaim?.Value 
                        }
                    });
                }
                
                // Debug: Log thông tin user và roles
                var roles = User.Claims.Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value).ToList();
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

                // Tìm staff record của user hiện tại
                var staff = await _staffManagementService.GetStaffByUserIdAsync(userId);
                if (staff == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Không tìm thấy thông tin staff",
                        debug = new {
                            userId = userId,
                            roles = roles,
                            isAuthenticated = isAuthenticated,
                            claims = allClaims
                        }
                    });
                }

                return Ok(new {
                    success = true,
                    message = "Lấy thông tin staff hiện tại thành công",
                    data = staff,
                    debug = new {
                        userId = userId,
                        roles = roles,
                        isAuthenticated = isAuthenticated
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        #endregion

        #region Validation APIs

        /// <summary>
        /// Kiểm tra người dùng có thể được gán vào trung tâm không
        /// </summary>
        /// <param name="userId">ID người dùng</param>
        /// <param name="centerId">ID trung tâm</param>
        /// <returns>Kết quả kiểm tra</returns>
        [HttpGet("validate/user-assignment")]
        public async Task<IActionResult> ValidateUserAssignment([FromQuery] int userId, [FromQuery] int centerId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });

                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                var canAssign = await _staffManagementService.CanUserBeAssignedToCenterAsync(userId, centerId);
                
                return Ok(new { 
                    success = true, 
                    message = "Kiểm tra khả năng gán người dùng thành công",
                    data = new { canAssign, userId, centerId }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        #endregion
    }
}
