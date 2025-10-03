using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkOrderController : ControllerBase
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly IBookingRepository _bookingRepository;

        public WorkOrderController(IWorkOrderRepository workOrderRepository, IBookingRepository bookingRepository)
        {
            _workOrderRepository = workOrderRepository;
            _bookingRepository = bookingRepository;
        }

        [HttpGet("by-booking/{bookingId:int}")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var wo = await _workOrderRepository.GetByBookingIdAsync(bookingId);
            if (wo == null) return NotFound(new { success = false, message = "Không tìm thấy work order cho booking này" });
            // Tránh vòng lặp tuần hoàn JSON: chỉ trả về các trường cần thiết
            var data = new
            {
                workOrderId = wo.WorkOrderId,
                bookingId = wo.BookingId,
                technicianId = wo.TechnicianId,
                customerId = wo.CustomerId,
                vehicleId = wo.VehicleId,
                centerId = wo.CenterId,
                serviceId = wo.ServiceId,
                technicianName = wo.Technician?.User?.FullName,
                status = wo.Status,
                
                createdAt = wo.CreatedAt,
                updatedAt = wo.UpdatedAt,
                
            };
            return Ok(new { success = true, data });
        }

        [HttpPost]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId);
            if (booking == null) return BadRequest(new { success = false, message = "Booking không tồn tại" });

            var existing = await _workOrderRepository.GetByBookingIdAsync(request.BookingId);
            if (existing != null) return Conflict(new { success = false, message = "Booking đã có work order" });

            var wo = new WorkOrder
            {
                BookingId = request.BookingId,
                TechnicianId = request.TechnicianId,
                CustomerId = null,
                VehicleId = null,
                CenterId = null,
                ServiceId = null,
                Status = request.Status ?? "NOT_STARTED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _workOrderRepository.CreateAsync(wo);
            return CreatedAtAction(nameof(GetByBooking), new { bookingId = created.BookingId }, new { success = true, data = created });
        }

        [HttpPost("{id}/start")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> Start(int id)
        {
            var wo = await _workOrderRepository.GetByIdAsync(id);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (!string.Equals(wo.Status, "NOT_STARTED", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Chỉ work order NOT_STARTED mới có thể start" });
            wo.Status = "IN_PROGRESS";
            wo.UpdatedAt = DateTime.UtcNow;
            await _workOrderRepository.UpdateAsync(wo);
            return Ok(new { success = true, message = "Đã bắt đầu work order", data = wo });
        }

        [HttpPost("{id}/complete")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> Complete(int id)
        {
            var wo = await _workOrderRepository.GetByIdAsync(id);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (!string.Equals(wo.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Chỉ work order IN_PROGRESS mới có thể complete" });
            wo.Status = "COMPLETED";
            wo.UpdatedAt = DateTime.UtcNow;
            await _workOrderRepository.UpdateAsync(wo);
            return Ok(new { success = true, message = "Đã hoàn tất work order", data = wo });
        }

        public class WorkOrderNoteRequest { public string Text { get; set; } public string ImageUrl { get; set; } }

        [HttpPost("{id}/notes")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> AddNote(int id, [FromBody] WorkOrderNoteRequest request)
        {
            var wo = await _workOrderRepository.GetByIdAsync(id);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            // Work notes removed from WorkOrder; ignored
            wo.UpdatedAt = DateTime.UtcNow;
            await _workOrderRepository.UpdateAsync(wo);
            return Ok(new { success = true, message = "Đã thêm ghi chú", data = wo });
        }
    }
}


