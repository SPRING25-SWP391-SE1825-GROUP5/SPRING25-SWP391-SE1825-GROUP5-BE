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
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập đều có thể xem
    public class TechnicianController : ControllerBase
    {
        private readonly ITechnicianService _technicianService;
        private readonly ITimeSlotService _timeSlotService;
        private readonly IBookingService _bookingService;

        public TechnicianController(ITechnicianService technicianService, ITimeSlotService timeSlotService, IBookingService bookingService)
        {
            _technicianService = technicianService;
            _timeSlotService = timeSlotService;
            _bookingService = bookingService;
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

    }

}
