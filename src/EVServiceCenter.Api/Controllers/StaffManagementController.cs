using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
    [Authorize] // Cho phép tất cả user đã đăng nhập, các endpoint sẽ tự set authorization
    public class StaffManagementController : ControllerBase
    {
        private readonly IStaffManagementService _staffManagementService;
        private readonly EVServiceCenter.Domain.Interfaces.ITechnicianRepository _technicianRepository;

        public StaffManagementController(IStaffManagementService staffManagementService, EVServiceCenter.Domain.Interfaces.ITechnicianRepository technicianRepository)
        {
            _staffManagementService = staffManagementService;
            _technicianRepository = technicianRepository;
        }

        #region Current User APIs


        #endregion

        #region Employee Management (Staff + Technician)

        /// <summary>
        /// Lấy danh sách user có role STAFF/TECHNICIAN nhưng chưa có bản ghi trong bảng Staff/Technician
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (tên, email, số điện thoại)</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
        /// <returns>Danh sách user chưa có bản ghi nhân viên</returns>
        [HttpGet("employees/available-users")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult> GetAvailableUsersForEmployee(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _staffManagementService.GetAvailableUsersForEmployeeAsync(pageNumber, pageSize, searchTerm, isActive);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách user có thể làm nhân viên thành công",
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
        /// Lấy danh sách tất cả nhân viên (Staff + Technician) theo trung tâm hoặc chưa có centerId
        /// </summary>
        /// <param name="centerId">ID trung tâm (bắt buộc nếu unassigned=false)</param>
        /// <param name="unassigned">Lấy nhân viên chưa có centerId (mặc định: false)</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (tên, email, số điện thoại)</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
        /// <returns>Danh sách nhân viên</returns>
        [HttpGet("employees")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult> GetCenterEmployees(
            [FromQuery] int? centerId = null,
            [FromQuery] bool unassigned = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                // Validation
                if (!unassigned && (!centerId.HasValue || centerId <= 0))
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ hoặc cần thiết khi unassigned=false" });

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _staffManagementService.GetCenterEmployeesAsync(
                    unassigned ? null : centerId, 
                    pageNumber, 
                    pageSize, 
                    searchTerm, 
                    isActive);
                
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
        /// Gán nhân viên vào center (dùng cho cả STAFF và TECHNICIAN)
        /// Có thể gán nhiều người cùng lúc
        /// </summary>
        /// <param name="request">Danh sách userIds và centerId</param>
        /// <returns>Danh sách nhân viên đã được gán</returns>
        [HttpPost("assign-employees")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult> AssignEmployeesToCenter([FromBody] AssignEmployeesToCenterRequest request)
        {
            try
            {
                if (request.UserIds == null || request.UserIds.Count == 0)
                    return BadRequest(new { success = false, message = "Danh sách User ID không được để trống" });
                if (request.CenterId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                var results = await _staffManagementService.AssignEmployeesToCenterAsync(request.UserIds, request.CenterId);
                
                return Ok(new { 
                    success = true, 
                    message = "Gán nhân viên vào trung tâm thành công",
                    data = results
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

        #endregion

        #region Validation APIs


        #endregion
    }

    #region Request Models

    public class AssignEmployeesToCenterRequest
    {
        public List<int> UserIds { get; set; } = new List<int>();
        public int CenterId { get; set; }
    }

    #endregion
}
