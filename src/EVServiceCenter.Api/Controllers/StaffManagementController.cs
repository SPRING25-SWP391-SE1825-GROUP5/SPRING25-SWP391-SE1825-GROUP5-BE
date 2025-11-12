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
    [Authorize]
    public class StaffManagementController : ControllerBase
    {
        private readonly IStaffManagementService _staffManagementService;
        private readonly EVServiceCenter.Domain.Interfaces.ITechnicianRepository _technicianRepository;

        public StaffManagementController(IStaffManagementService staffManagementService, EVServiceCenter.Domain.Interfaces.ITechnicianRepository technicianRepository)
        {
            _staffManagementService = staffManagementService;
            _technicianRepository = technicianRepository;
        }

        [HttpGet("staff/current")]
        [Authorize(Roles = "STAFF,MANAGER,ADMIN")]
        public async Task<IActionResult> GetCurrentStaff()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("userId")?.Value
                                   ?? User.FindFirst("sub")?.Value
                                   ?? User.FindFirst("nameid")?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });
                }

                var staff = await _staffManagementService.GetStaffByUserIdAsync(userId);

                return Ok(new {
                    success = true,
                    message = "Lấy thông tin nhân viên hiện tại thành công",
                    data = new {
                        staffId = staff.StaffId,
                        userId = staff.UserId,
                        fullName = staff.UserFullName,
                        email = staff.UserEmail,
                        phoneNumber = staff.UserPhoneNumber,
                        centerId = staff.CenterId,
                        centerName = staff.CenterName,
                        isActive = staff.IsActive,
                        createdAt = staff.CreatedAt
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

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
    }

    public class AssignEmployeesToCenterRequest
    {
        public List<int> UserIds { get; set; } = new List<int>();
        public int CenterId { get; set; }
    }
}