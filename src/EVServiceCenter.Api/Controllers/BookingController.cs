using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
 

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
        {
        _bookingService = bookingService;
        }

        /// <summary>
        /// Lấy thông tin khả dụng của trung tâm theo ngày và dịch vụ
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="date">Ngày (YYYY-MM-DD)</param>
        /// <param name="serviceIds">Danh sách ID dịch vụ (comma-separated)</param>
        /// <returns>Thông tin khả dụng</returns>
        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability(
            [FromQuery] int centerId,
            [FromQuery] string date,
            [FromQuery] string serviceIds = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                // Parse service IDs if provided
                var serviceIdList = new List<int>();
                if (!string.IsNullOrWhiteSpace(serviceIds))
                {
                    var serviceIdStrings = serviceIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var serviceIdString in serviceIdStrings)
                    {
                        if (int.TryParse(serviceIdString.Trim(), out var serviceId))
                        {
                            serviceIdList.Add(serviceId);
                        }
                    }
                }

                var availability = await _bookingService.GetAvailabilityAsync(centerId, bookingDate, serviceIdList);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin khả dụng thành công",
                    data = availability
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
        /// Lấy danh sách thời gian khả dụng dựa trên WeeklySchedule và technician timeslots
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="date">Ngày (YYYY-MM-DD)</param>
        /// <param name="technicianId">ID kỹ thuật viên (optional)</param>
        /// <param name="serviceIds">Danh sách ID dịch vụ (comma-separated, optional)</param>
        /// <returns>Danh sách thời gian khả dụng</returns>
        [HttpGet("available-times")]
        public async Task<IActionResult> GetAvailableTimes(
            [FromQuery] int centerId,
            [FromQuery] string date,
            [FromQuery] int? technicianId = null,
            [FromQuery] string serviceIds = null)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                // Validate date is not in the past
                if (bookingDate < DateOnly.FromDateTime(DateTime.Today))
                    return BadRequest(new { success = false, message = "Không thể đặt lịch cho ngày trong quá khứ" });

                // Parse service IDs if provided
                var serviceIdList = new List<int>();
                if (!string.IsNullOrWhiteSpace(serviceIds))
                {
                    var serviceIdStrings = serviceIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var serviceIdString in serviceIdStrings)
                    {
                        if (int.TryParse(serviceIdString.Trim(), out var serviceId))
                        {
                            serviceIdList.Add(serviceId);
                        }
                    }
                }

                var availableTimes = await _bookingService.GetAvailableTimesAsync(centerId, bookingDate, technicianId, serviceIdList);

                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách thời gian khả dụng thành công",
                    data = availableTimes
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
        /// Tạm giữ time slot để tránh conflict khi đặt lịch
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <param name="date">Ngày (YYYY-MM-DD)</param>
        /// <param name="slotId">ID slot</param>
        /// <param name="bookingId">ID booking (optional)</param>
        /// <returns>Kết quả tạm giữ</returns>
        [HttpPost("reserve-slot")]
        public async Task<IActionResult> ReserveTimeSlot(
            [FromQuery] int technicianId,
            [FromQuery] string date,
            [FromQuery] int slotId,
            [FromQuery] int? bookingId = null)
        {
            try
            {
                if (technicianId <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                if (slotId <= 0)
                    return BadRequest(new { success = false, message = "ID slot không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                var result = await _bookingService.ReserveTimeSlotAsync(technicianId, bookingDate, slotId, bookingId);

                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Tạm giữ time slot thành công",
                        data = new { technicianId, date = bookingDate, slotId, bookingId }
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Không thể tạm giữ time slot. Slot có thể đã được đặt hoặc không khả dụng." 
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
        /// Giải phóng time slot đã tạm giữ
        /// </summary>
        /// <param name="technicianId">ID kỹ thuật viên</param>
        /// <param name="date">Ngày (YYYY-MM-DD)</param>
        /// <param name="slotId">ID slot</param>
        /// <returns>Kết quả giải phóng</returns>
        [HttpPost("release-slot")]
        public async Task<IActionResult> ReleaseTimeSlot(
            [FromQuery] int technicianId,
            [FromQuery] string date,
            [FromQuery] int slotId)
        {
            try
            {
                if (technicianId <= 0)
                    return BadRequest(new { success = false, message = "ID kỹ thuật viên không hợp lệ" });

                if (slotId <= 0)
                    return BadRequest(new { success = false, message = "ID slot không hợp lệ" });

                if (!DateOnly.TryParse(date, out var bookingDate))
                    return BadRequest(new { success = false, message = "Ngày không đúng định dạng YYYY-MM-DD" });

                var result = await _bookingService.ReleaseTimeSlotAsync(technicianId, bookingDate, slotId);

                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Giải phóng time slot thành công",
                        data = new { technicianId, date = bookingDate, slotId }
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Không thể giải phóng time slot." 
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
        /// Tạo đặt lịch mới
        /// </summary>
        /// <param name="request">Thông tin đặt lịch</param>
        /// <returns>Thông tin đặt lịch đã tạo</returns>
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
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

                var booking = await _bookingService.CreateBookingAsync(request);
                
                return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, new { 
                    success = true, 
                    message = "Tạo đặt lịch thành công",
                    data = booking
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
        /// Lấy thông tin đặt lịch theo ID
        /// </summary>
        /// <param name="id">ID đặt lịch</param>
        /// <returns>Thông tin đặt lịch</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID đặt lịch không hợp lệ" });

                var booking = await _bookingService.GetBookingByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin đặt lịch thành công",
                    data = booking
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
        /// Tự động gán kỹ thuật viên cho booking 1 slot (khi user không chọn)
        /// </summary>
        [HttpPost("{id}/auto-assign-technician")]
        public async Task<IActionResult> AutoAssignTechnician(int id)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id);
                if (booking == null)
                    return NotFound(new { success = false, message = "Đặt lịch không tồn tại." });

                var available = await _bookingService.GetAvailableTimesAsync(booking.CenterId, booking.BookingDate, null, null);
                var pick = available.AvailableTimeSlots
                    .FirstOrDefault(t => t.SlotId == booking.SlotId && t.IsRealtimeAvailable);

                if (pick == null || pick.TechnicianId == null)
                    return BadRequest(new { success = false, message = "Không có kỹ thuật viên khả dụng cho slot đã chọn." });

                var assignReq = new AssignBookingTimeSlotsRequest
                {
                    TimeSlots = new List<BookingTimeSlotRequest>
                    {
                        new BookingTimeSlotRequest { SlotId = booking.SlotId, TechnicianId = pick.TechnicianId.Value }
                    }
                };

                var updated = await _bookingService.AssignBookingTimeSlotsAsync(id, assignReq);
                return Ok(new { success = true, message = "Gán kỹ thuật viên thành công", data = updated });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Hủy đặt lịch (giải phóng slot, cập nhật trạng thái)
        /// </summary>
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var req = new UpdateBookingStatusRequest { Status = "CANCELLED" };
                var updated = await _bookingService.UpdateBookingStatusAsync(id, req);
                return Ok(new { success = true, message = "Hủy đặt lịch thành công", data = updated });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đặt lịch (Staff/Admin only)
        /// </summary>
        /// <param name="id">ID đặt lịch</param>
        /// <param name="request">Thông tin cập nhật trạng thái</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{id}/status")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID đặt lịch không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var booking = await _bookingService.UpdateBookingStatusAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật trạng thái đặt lịch thành công",
                    data = booking
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
        /// Gán dịch vụ cho đặt lịch (Staff/Admin only)
        /// </summary>
        /// <param name="id">ID đặt lịch</param>
        /// <param name="request">Danh sách dịch vụ</param>
        /// <returns>Kết quả gán dịch vụ</returns>
        [HttpPost("{id}/services")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> AssignBookingServices(int id, [FromBody] AssignBookingServicesRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID đặt lịch không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var booking = await _bookingService.AssignBookingServicesAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Gán dịch vụ cho đặt lịch thành công",
                    data = booking
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
        /// Gán time slots cho đặt lịch (Staff/Admin only)
        /// </summary>
        /// <param name="id">ID đặt lịch</param>
        /// <param name="request">Danh sách time slots</param>
        /// <returns>Kết quả gán time slots</returns>
        [HttpPost("{id}/assign-slots")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> AssignBookingTimeSlots(int id, [FromBody] AssignBookingTimeSlotsRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID đặt lịch không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var booking = await _bookingService.AssignBookingTimeSlotsAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Gán time slots cho đặt lịch thành công",
                    data = booking
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

        // Payment API đã được gỡ bỏ theo yêu cầu
    }
}

