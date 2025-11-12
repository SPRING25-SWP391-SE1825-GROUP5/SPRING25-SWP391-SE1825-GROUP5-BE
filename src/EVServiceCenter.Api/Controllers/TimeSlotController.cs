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
    public class TimeSlotController : ControllerBase
    {
        private readonly ITimeSlotService _timeSlotService;

        public TimeSlotController(ITimeSlotService timeSlotService)
        {
            _timeSlotService = timeSlotService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeSlots([FromQuery] bool? active = null)
        {
            try
            {
                List<TimeSlotResponse> timeSlots;

                if (active.HasValue)
                {
                    if (active.Value)
                    {
                        timeSlots = await _timeSlotService.GetActiveTimeSlotsAsync();
                    }
                    else
                    {
                        var allTimeSlots = await _timeSlotService.GetAllTimeSlotsAsync();
                        timeSlots = allTimeSlots.Where(ts => !ts.IsActive).ToList();
                    }
                }
                else
                {
                    timeSlots = await _timeSlotService.GetAllTimeSlotsAsync();
                }

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách time slots thành công",
                    data = timeSlots,
                    filter = active.HasValue ? (active.Value ? "active" : "inactive") : "all"
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

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateTimeSlot([FromBody] CreateTimeSlotRequest request)
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

                var timeSlot = await _timeSlotService.CreateTimeSlotAsync(request);

                return CreatedAtAction(nameof(GetTimeSlots), new { active = true }, new {
                    success = true,
                    message = "Tạo time slot thành công",
                    data = timeSlot
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            try
            {
                var ts = await _timeSlotService.GetByIdAsync(id);
                return Ok(new { success = true, data = ts });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateTimeSlotRequest request)
        {
            try
            {
                var ts = await _timeSlotService.UpdateTimeSlotAsync(id, request);
                return Ok(new { success = true, message = "Cập nhật time slot thành công", data = ts });
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

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var ok = await _timeSlotService.DeleteTimeSlotAsync(id);
            if (!ok) return BadRequest(new { success = false, message = "Không thể xóa slot (đang được sử dụng hoặc không tồn tại)" });
            return Ok(new { success = true });
        }
    }
}
