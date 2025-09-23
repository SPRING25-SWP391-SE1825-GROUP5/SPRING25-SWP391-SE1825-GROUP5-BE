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
    [Authorize(Policy = "AuthenticatedUser")]
    public class CenterScheduleController : ControllerBase
    {
        private readonly ICenterScheduleService _centerScheduleService;

        public CenterScheduleController(ICenterScheduleService centerScheduleService)
        {
            _centerScheduleService = centerScheduleService;
        }

        /// <summary>
        /// Tạo center schedule mới
        /// </summary>
        /// <param name="request">Thông tin tạo center schedule</param>
        /// <returns>Kết quả tạo center schedule</returns>
        [HttpPost]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> CreateCenterSchedule([FromBody] CreateCenterScheduleRequest request)
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

                var result = await _centerScheduleService.CreateCenterScheduleAsync(request);
                
                return CreatedAtAction(nameof(GetCenterScheduleById), new { centerScheduleId = result.CenterScheduleId }, new { 
                    success = true, 
                    message = "Tạo center schedule thành công",
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
        /// Lấy danh sách center schedules theo center
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <param name="dayOfWeek">Ngày trong tuần (0-6)</param>
        /// <returns>Danh sách center schedules</returns>
        [HttpGet("by-center/{centerId}")]
        public async Task<IActionResult> GetCenterSchedulesByCenter(
            int centerId, 
            [FromQuery] byte? dayOfWeek = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "CenterId phải lớn hơn 0" });

                if (dayOfWeek.HasValue && (dayOfWeek < 0 || dayOfWeek > 6))
                    return BadRequest(new { success = false, message = "DayOfWeek phải từ 0-6" });

                var schedules = await _centerScheduleService.GetCenterSchedulesByCenterAsync(centerId, dayOfWeek);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách center schedules thành công",
                    data = schedules,
                    total = schedules.Count
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
        /// Lấy danh sách center schedules đang hoạt động
        /// </summary>
        /// <returns>Danh sách center schedules đang hoạt động</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCenterSchedules()
        {
            try
            {
                var schedules = await _centerScheduleService.GetActiveCenterSchedulesAsync();
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách active center schedules thành công",
                    data = schedules,
                    total = schedules.Count
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
        /// Lấy center schedule theo ID
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <returns>Thông tin center schedule</returns>
        [HttpGet("{centerScheduleId}")]
        public async Task<IActionResult> GetCenterScheduleById(int centerScheduleId)
        {
            try
            {
                if (centerScheduleId <= 0)
                    return BadRequest(new { success = false, message = "CenterScheduleId phải lớn hơn 0" });

                var schedule = await _centerScheduleService.GetCenterScheduleByIdAsync(centerScheduleId);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin center schedule thành công",
                    data = schedule
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
        /// Lấy danh sách center schedules có sẵn trong khoảng thời gian
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <param name="dayOfWeek">Ngày trong tuần (0-6)</param>
        /// <param name="startTime">Thời gian bắt đầu (HH:mm)</param>
        /// <param name="endTime">Thời gian kết thúc (HH:mm)</param>
        /// <returns>Danh sách center schedules có sẵn</returns>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSchedules(
            [FromQuery] int centerId,
            [FromQuery] byte dayOfWeek,
            [FromQuery] string startTime,
            [FromQuery] string endTime)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "CenterId phải lớn hơn 0" });

                if (dayOfWeek < 0 || dayOfWeek > 6)
                    return BadRequest(new { success = false, message = "DayOfWeek phải từ 0-6" });

                if (!TimeOnly.TryParse(startTime, out var start))
                    return BadRequest(new { success = false, message = "StartTime không đúng định dạng HH:mm" });

                if (!TimeOnly.TryParse(endTime, out var end))
                    return BadRequest(new { success = false, message = "EndTime không đúng định dạng HH:mm" });

                var schedules = await _centerScheduleService.GetAvailableSchedulesAsync(centerId, dayOfWeek, start, end);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách available schedules thành công",
                    data = schedules,
                    total = schedules.Count
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
        /// Cập nhật center schedule
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{centerScheduleId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> UpdateCenterSchedule(int centerScheduleId, [FromBody] UpdateCenterScheduleRequest request)
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

                if (centerScheduleId <= 0)
                    return BadRequest(new { success = false, message = "CenterScheduleId phải lớn hơn 0" });

                var result = await _centerScheduleService.UpdateCenterScheduleAsync(centerScheduleId, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật center schedule thành công",
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
        /// Xóa center schedule
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{centerScheduleId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> DeleteCenterSchedule(int centerScheduleId)
        {
            try
            {
                if (centerScheduleId <= 0)
                    return BadRequest(new { success = false, message = "CenterScheduleId phải lớn hơn 0" });

                var result = await _centerScheduleService.DeleteCenterScheduleAsync(centerScheduleId);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Xóa center schedule thành công"
                    });
                }
                else
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Không tìm thấy center schedule để xóa"
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
        /// Cập nhật capacity left khi có booking
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <param name="capacityUsed">Số lượng capacity đã sử dụng</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPatch("{centerScheduleId}/capacity")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> UpdateCapacityLeft(int centerScheduleId, [FromBody] int capacityUsed)
        {
            try
            {
                if (centerScheduleId <= 0)
                    return BadRequest(new { success = false, message = "CenterScheduleId phải lớn hơn 0" });

                if (capacityUsed <= 0)
                    return BadRequest(new { success = false, message = "CapacityUsed phải lớn hơn 0" });

                var result = await _centerScheduleService.UpdateCapacityLeftAsync(centerScheduleId, capacityUsed);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật capacity thành công"
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
