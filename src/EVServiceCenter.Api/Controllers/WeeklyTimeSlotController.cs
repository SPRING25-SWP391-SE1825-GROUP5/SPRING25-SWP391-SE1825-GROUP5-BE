using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeeklyTimeSlotController : ControllerBase
    {
        private readonly IWeeklyTimeSlotService _weeklyTimeSlotService;

        public WeeklyTimeSlotController(IWeeklyTimeSlotService weeklyTimeSlotService)
        {
            _weeklyTimeSlotService = weeklyTimeSlotService;
        }

        /// <summary>
        /// Tạo time slot tổng theo tuần cho khách hàng
        /// </summary>
        /// <param name="request">Thông tin tạo weekly time slot</param>
        /// <returns>Kết quả tạo weekly time slot</returns>
        [HttpPost("create-weekly-slots")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateWeeklyTimeSlots([FromBody] CreateWeeklyTimeSlotRequest request)
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

                var result = await _weeklyTimeSlotService.CreateWeeklyTimeSlotsAsync(request);
                
                return Ok(new { 
                    success = true, 
                    message = "Tạo weekly time slots thành công",
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
        /// Lấy danh sách weekly time slots theo center
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <param name="startDate">Ngày bắt đầu (YYYY-MM-DD)</param>
        /// <param name="endDate">Ngày kết thúc (YYYY-MM-DD)</param>
        /// <returns>Danh sách weekly time slots</returns>
        [HttpGet("by-center/{centerId}")]
        public async Task<IActionResult> GetWeeklyTimeSlotsByLocation(
            int centerId, 
            [FromQuery] string? startDate = null, 
            [FromQuery] string? endDate = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "CenterId phải lớn hơn 0" });

                DateOnly? start = null;
                DateOnly? end = null;

                if (!string.IsNullOrEmpty(startDate))
                {
                    if (!DateOnly.TryParse(startDate, out var parsedStart))
                        return BadRequest(new { success = false, message = "Ngày bắt đầu không đúng định dạng YYYY-MM-DD" });
                    start = parsedStart;
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    if (!DateOnly.TryParse(endDate, out var parsedEnd))
                        return BadRequest(new { success = false, message = "Ngày kết thúc không đúng định dạng YYYY-MM-DD" });
                    end = parsedEnd;
                }

                var timeSlots = await _weeklyTimeSlotService.GetWeeklyTimeSlotsByLocationAsync(centerId, start, end);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách weekly time slots thành công",
                    data = timeSlots,
                    total = timeSlots.Count
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
        /// Lấy danh sách weekly time slots theo technician
        /// </summary>
        /// <param name="technicianId">ID của technician</param>
        /// <param name="startDate">Ngày bắt đầu (YYYY-MM-DD)</param>
        /// <param name="endDate">Ngày kết thúc (YYYY-MM-DD)</param>
        /// <returns>Danh sách weekly time slots</returns>
        [HttpGet("by-technician/{technicianId}")]
        public async Task<IActionResult> GetWeeklyTimeSlotsByTechnician(
            int technicianId, 
            [FromQuery] string? startDate = null, 
            [FromQuery] string? endDate = null)
        {
            try
            {
                if (technicianId <= 0)
                    return BadRequest(new { success = false, message = "TechnicianId phải lớn hơn 0" });

                DateOnly? start = null;
                DateOnly? end = null;

                if (!string.IsNullOrEmpty(startDate))
                {
                    if (!DateOnly.TryParse(startDate, out var parsedStart))
                        return BadRequest(new { success = false, message = "Ngày bắt đầu không đúng định dạng YYYY-MM-DD" });
                    start = parsedStart;
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    if (!DateOnly.TryParse(endDate, out var parsedEnd))
                        return BadRequest(new { success = false, message = "Ngày kết thúc không đúng định dạng YYYY-MM-DD" });
                    end = parsedEnd;
                }

                var timeSlots = await _weeklyTimeSlotService.GetWeeklyTimeSlotsByTechnicianAsync(technicianId, start, end);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách weekly time slots thành công",
                    data = timeSlots,
                    total = timeSlots.Count
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
        /// Lấy danh sách weekly time slots đang hoạt động
        /// </summary>
        /// <returns>Danh sách weekly time slots đang hoạt động</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveWeeklyTimeSlots()
        {
            try
            {
                var timeSlots = await _weeklyTimeSlotService.GetActiveWeeklyTimeSlotsAsync();
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách active weekly time slots thành công",
                    data = timeSlots,
                    total = timeSlots.Count
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
        /// Xóa weekly time slot
        /// </summary>
        /// <param name="weeklyScheduleId">ID của weekly schedule</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{weeklyScheduleId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteWeeklyTimeSlot(int weeklyScheduleId)
        {
            try
            {
                if (weeklyScheduleId <= 0)
                    return BadRequest(new { success = false, message = "WeeklyScheduleId phải lớn hơn 0" });

                var result = await _weeklyTimeSlotService.DeleteWeeklyTimeSlotAsync(weeklyScheduleId);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Xóa weekly time slot thành công"
                    });
                }
                else
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Không tìm thấy weekly time slot để xóa"
                    });
                }
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
        /// Cập nhật weekly time slot
        /// </summary>
        /// <param name="weeklyScheduleId">ID của weekly schedule</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{weeklyScheduleId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateWeeklyTimeSlot(int weeklyScheduleId, [FromBody] CreateWeeklyTimeSlotRequest request)
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

                if (weeklyScheduleId <= 0)
                    return BadRequest(new { success = false, message = "WeeklyScheduleId phải lớn hơn 0" });

                var result = await _weeklyTimeSlotService.UpdateWeeklyTimeSlotAsync(weeklyScheduleId, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật weekly time slot thành công",
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
    }
}
