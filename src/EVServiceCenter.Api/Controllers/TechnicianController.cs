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
        [HttpGet]
        public async Task<IActionResult> GetAllTechnicians(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? centerId = null)
        {
            try
            {
                // Validate pagination parameters
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
        /// Danh sách booking theo ngày của kỹ thuật viên (kèm WorkOrder)
        /// </summary>
        [HttpGet("{technicianId}/bookings")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> GetBookingsByDate(int technicianId, [FromQuery] string date)
        {
            if (technicianId <= 0)
                return BadRequest(new { success = false, message = "TechnicianId không hợp lệ" });
            if (!DateOnly.TryParse(date, out var d))
                return BadRequest(new { success = false, message = "Ngày không hợp lệ (YYYY-MM-DD)" });

            var data = await _technicianService.GetBookingsByDateAsync(technicianId, d);
            return Ok(new { success = true, message = "Lấy danh sách booking theo ngày thành công", data });
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
        /// Lấy lịch làm việc của kỹ thuật viên theo ngày
        /// </summary>
        /// <param name="id">ID kỹ thuật viên</param>
        /// <param name="date">Ngày cần xem lịch (YYYY-MM-DD)</param>
        /// <returns>Lịch làm việc của kỹ thuật viên</returns>
        [HttpGet("{id}/availability")]
        public async Task<IActionResult> GetTechnicianAvailability(int id, [FromQuery] string date)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                if (string.IsNullOrWhiteSpace(date))
                    return BadRequest(new { success = false, message = "Ngày là bắt buộc (YYYY-MM-DD)" });

                if (!DateOnly.TryParse(date, out DateOnly workDate))
                    return BadRequest(new { success = false, message = "Định dạng ngày không hợp lệ. Vui lòng sử dụng YYYY-MM-DD" });

                var availability = await _technicianService.GetTechnicianAvailabilityAsync(id, workDate);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy lịch làm việc thành công",
                    data = availability
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
        /// Khả dụng của kỹ thuật viên theo dịch vụ trong 1 ngày (lọc theo center + service)
        /// </summary>
        [HttpGet("{technicianId}/availability-by-service")]
        public async Task<IActionResult> GetTechnicianAvailabilityByService(
            int technicianId,
            [FromQuery] int centerId,
            [FromQuery] string date,
            [FromQuery] int serviceId)
        {
            if (technicianId <= 0 || centerId <= 0 || serviceId <= 0)
                return BadRequest(new { success = false, message = "Thiếu tham số hoặc không hợp lệ" });
            if (!DateOnly.TryParse(date, out var d))
                return BadRequest(new { success = false, message = "Ngày không hợp lệ (YYYY-MM-DD)" });

            var availability = await _bookingService.GetAvailabilityAsync(centerId, d, new System.Collections.Generic.List<int> { serviceId });
            var slots = availability?.TimeSlots ?? new System.Collections.Generic.List<EVServiceCenter.Application.Models.Responses.TimeSlotAvailability>();
            var availableSlotIds = slots
                .Where(ts => ts.IsAvailable && ts.AvailableTechnicians.Any(t => t.TechnicianId == technicianId && t.IsAvailable))
                .Select(ts => ts.SlotId)
                .ToList();

            return Ok(new { success = true, data = new { technicianId, centerId, serviceId, date = d, slotIds = availableSlotIds } });
        }

        /// <summary>
        /// Khả dụng của kỹ thuật viên theo trung tâm trong 1 ngày (gom theo technician)
        /// </summary>
        [HttpGet("centers/{centerId}/technicians/availability")]
        public async Task<IActionResult> GetCenterTechniciansAvailability(
            int centerId,
            [FromQuery] string date,
            [FromQuery] int serviceId)
        {
            if (centerId <= 0 || serviceId <= 0)
                return BadRequest(new { success = false, message = "Thiếu tham số hoặc không hợp lệ" });
            if (!DateOnly.TryParse(date, out var d))
                return BadRequest(new { success = false, message = "Ngày không hợp lệ (YYYY-MM-DD)" });

            var availability = await _bookingService.GetAvailabilityAsync(centerId, d, new System.Collections.Generic.List<int> { serviceId });
            var slots = availability?.TimeSlots ?? new System.Collections.Generic.List<EVServiceCenter.Application.Models.Responses.TimeSlotAvailability>();

            var dict = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();
            foreach (var ts in slots.Where(s => s.IsAvailable))
            {
                foreach (var tech in ts.AvailableTechnicians.Where(t => t.IsAvailable))
                {
                    if (!dict.TryGetValue(tech.TechnicianId, out var list))
                    {
                        list = new System.Collections.Generic.List<int>();
                        dict[tech.TechnicianId] = list;
                    }
                    list.Add(ts.SlotId);
                }
            }

            return Ok(new { success = true, data = new { centerId, serviceId, date = d, technicians = dict } });
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

        /// <summary>
        /// Thêm/cập nhật danh sách kỹ năng cho kỹ thuật viên (ADMIN)
        /// </summary>
        [HttpPost("{technicianId}/skills")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpsertSkills(int technicianId, [FromBody] UpsertTechnicianSkillsRequest request)
        {
            try
            {
                if (!ModelState.IsValid || request == null)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }
                await _technicianService.UpsertSkillsAsync(technicianId, request);
                return Ok(new { success = true, message = "Cập nhật kỹ năng thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xoá một kỹ năng của kỹ thuật viên (ADMIN)
        /// </summary>
        [HttpDelete("{technicianId}/skills/{skillId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RemoveSkill(int technicianId, int skillId)
        {
            try
            {
                await _technicianService.RemoveSkillAsync(technicianId, skillId);
                return Ok(new { success = true, message = "Xoá kỹ năng thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách kỹ năng của technician
        /// </summary>
        /// <param name="technicianId">ID của technician</param>
        /// <returns>Danh sách kỹ năng</returns>
        [HttpGet("{technicianId}/skills")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianSkills(int technicianId)
        {
            try
            {
                var skills = await _technicianService.GetTechnicianSkillsAsync(technicianId);
                return Ok(new 
                { 
                    success = true, 
                    message = "Lấy danh sách kỹ năng technician thành công",
                    data = skills, 
                    total = skills.Count 
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

}
