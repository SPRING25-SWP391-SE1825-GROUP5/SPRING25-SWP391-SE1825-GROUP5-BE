using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập đều có thể xem
    public class TechnicianController : ControllerBase
    {
        private readonly ITechnicianService _technicianService;
        private readonly ITimeSlotService _timeSlotService;
        private readonly IBookingService _bookingService;
        private readonly ITechnicianDashboardService _dashboardService;

        public TechnicianController(
            ITechnicianService technicianService, 
            ITimeSlotService timeSlotService, 
            IBookingService bookingService,
            ITechnicianDashboardService dashboardService)
        {
            _technicianService = technicianService;
            _timeSlotService = timeSlotService;
            _bookingService = bookingService;
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Lấy danh sách tất cả kỹ thuật viên với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="centerId">Lọc theo trung tâm</param>
        /// <returns>Danh sách kỹ thuật viên</returns>
        [HttpGet("by-center/{centerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTechniciansByCenter(int centerId)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                var technicians = await _technicianService.GetAllTechniciansAsync(1, 100, null, centerId);
                
                return Ok(new { 
                    success = true, 
                    message = $"Lấy danh sách kỹ thuật viên của trung tâm {centerId} thành công",
                    data = technicians
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

        [HttpGet]
        public async Task<IActionResult> GetAllTechnicians(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? centerId = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _technicianService.GetAllTechniciansAsync(pageNumber, pageSize, searchTerm, centerId);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách kỹ thuật viên thành công",
                    data = result
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


        /// <summary>
        /// Danh sách booking của kỹ thuật viên
        /// </summary>
        [HttpGet("{technicianId}/bookings")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetBookings(int technicianId)
        {
            if (technicianId <= 0)
                return BadRequest(new { success = false, message = "TechnicianId không hợp lệ" });

            var data = await _technicianService.GetAllBookingsAsync(technicianId);
            return Ok(new { success = true, message = "Lấy danh sách booking thành công", data });
        }

        /// <summary>
        /// Chi tiết booking của kỹ thuật viên
        /// </summary>
        [HttpGet("{technicianId}/bookings/{bookingId}")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> GetBookingDetail(int technicianId, int bookingId)
        {
            if (technicianId <= 0)
                return BadRequest(new { success = false, message = "TechnicianId không hợp lệ" });

            if (bookingId <= 0)
                return BadRequest(new { success = false, message = "BookingId không hợp lệ" });

            var data = await _technicianService.GetBookingDetailAsync(technicianId, bookingId);
            return Ok(new { success = true, message = "Lấy thông tin chi tiết booking thành công", data });
        }

        /// <summary>
        /// Lấy thông tin kỹ thuật viên theo ID
        /// </summary>
        /// <param name="id">ID kỹ thuật viên</param>
        /// <returns>Thông tin kỹ thuật viên</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTechnicianById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                var technician = await _technicianService.GetTechnicianByIdAsync(id);
                
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
        /// Lấy danh sách timeslots của kỹ thuật viên
        /// </summary>
        /// <param name="id">ID kỹ thuật viên</param>
        /// <param name="active">Lọc theo trạng thái active (true/false/null = all)</param>
        /// <returns>Danh sách timeslots của kỹ thuật viên</returns>
        [HttpGet("{id}/timeslots")]
        public async Task<IActionResult> GetTechnicianTimeSlots(int id, [FromQuery] bool? active = null)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                // Verify technician exists
                var technician = await _technicianService.GetTechnicianByIdAsync(id);
                if (technician == null)
                    return NotFound(new { success = false, message = "Kỹ thuật viên không tồn tại." });

                var timeSlots = active.HasValue 
                    ? (active.Value ? await _timeSlotService.GetActiveTimeSlotsAsync() : await _timeSlotService.GetAllTimeSlotsAsync())
                    : await _timeSlotService.GetAllTimeSlotsAsync();
                
                return Ok(new
                {
                    success = true,
                    message = $"Lấy danh sách timeslots của kỹ thuật viên {id} thành công",
                    data = new
                    {
                        technicianId = id,
                        technicianName = technician.UserFullName,
                        timeSlots = timeSlots
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật lịch làm việc của kỹ thuật viên (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID kỹ thuật viên</param>
        /// <param name="request">Thông tin cập nhật lịch làm việc</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{id}/availability")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateTechnicianAvailability(int id, [FromBody] UpdateTechnicianAvailabilityRequest request)
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

                var result = await _technicianService.UpdateTechnicianAvailabilityAsync(id, request);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Cập nhật lịch làm việc thành công" 
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Không thể cập nhật lịch làm việc" 
                    });
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

        // ============================================================================
        // TECHNICIAN DASHBOARD APIs
        // ============================================================================

        /// <summary>
        /// Lấy thông tin dashboard cho kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <returns>Dashboard overview</returns>
        [HttpGet("{technicianId}/dashboard")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetDashboard(int technicianId)
        {
            try
            {
                var result = await _dashboardService.GetDashboardAsync(technicianId);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin dashboard thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách booking hôm nay của kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <returns>Danh sách booking hôm nay</returns>
        [HttpGet("{technicianId}/bookings/today")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetTodayBookings(int technicianId)
        {
            try
            {
                var result = await _dashboardService.GetTodayBookingsAsync(technicianId);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking hôm nay thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách booking đang chờ của kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <returns>Danh sách booking đang chờ</returns>
        [HttpGet("{technicianId}/bookings/pending")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetPendingBookings(int technicianId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _dashboardService.GetPendingBookingsAsync(technicianId, pageNumber, pageSize);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking đang chờ thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách booking đang xử lý của kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <returns>Danh sách booking đang xử lý</returns>
        [HttpGet("{technicianId}/bookings/in-progress")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetInProgressBookings(int technicianId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _dashboardService.GetInProgressBookingsAsync(technicianId, pageNumber, pageSize);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking đang xử lý thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách booking đã hoàn thành của kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <returns>Danh sách booking đã hoàn thành</returns>
        [HttpGet("{technicianId}/bookings/completed")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetCompletedBookings(int technicianId, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _dashboardService.GetCompletedBookingsAsync(technicianId, pageNumber, pageSize);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách booking đã hoàn thành thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê của kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <returns>Thống kê</returns>
        [HttpGet("{technicianId}/stats")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetStats(int technicianId)
        {
            try
            {
                var result = await _dashboardService.GetStatsAsync(technicianId);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy thống kê thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy hiệu suất của kỹ thuật viên
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <returns>Hiệu suất</returns>
        [HttpGet("{technicianId}/performance")]
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> GetPerformance(int technicianId)
        {
            try
            {
                var result = await _dashboardService.GetPerformanceAsync(technicianId);
                
                return Ok(new
                {
                    success = true,
                    message = "Lấy hiệu suất thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

    }

}
