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

        /// <summary>
        /// Lấy danh sách tất cả time slots
        /// </summary>
        /// <param name="active">Lọc theo trạng thái active (true/false/null)</param>
        /// <returns>Danh sách time slots</returns>
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
                        // Get all and filter inactive ones
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

        /// <summary>
        /// Tạo time slot mới (chỉ ADMIN)
        /// </summary>
        /// <param name="request">Thông tin time slot mới</param>
        /// <returns>Thông tin time slot đã tạo</returns>
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
    }
}
