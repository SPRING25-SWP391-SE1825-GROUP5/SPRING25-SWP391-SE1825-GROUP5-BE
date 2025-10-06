using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Configurations;
using Microsoft.Extensions.Options;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/reminders")]
    public class ReminderController : ControllerBase
    {
        private readonly IMaintenanceReminderRepository _repo;
        private readonly IEmailService _email;
        private readonly MaintenanceReminderOptions _options;

        public ReminderController(IMaintenanceReminderRepository repo, IEmailService email, IOptions<MaintenanceReminderOptions> options)
        {
            _repo = repo;
            _email = email;
            _options = options.Value;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List([FromQuery] int? customerId = null, [FromQuery] int? vehicleId = null, [FromQuery] string status = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var items = await _repo.QueryAsync(customerId, vehicleId, status, from, to);
            return Ok(new { success = true, data = items });
        }

        public class CreateReminderRequest { public int VehicleId { get; set; } public int? ServiceId { get; set; } public DateTime? DueDate { get; set; } public int? DueMileage { get; set; } }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReminderRequest req)
        {
            if (req == null || req.VehicleId <= 0) return BadRequest(new { success = false, message = "vehicleId bắt buộc" });
            var entity = new EVServiceCenter.Domain.Entities.MaintenanceReminder
            {
                VehicleId = req.VehicleId,
                ServiceId = req.ServiceId,
                DueDate = req.DueDate.HasValue ? DateOnly.FromDateTime(req.DueDate.Value) : null,
                DueMileage = req.DueMileage,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };
            var created = await _repo.CreateAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = created.ReminderId }, new { success = true, data = created });
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return NotFound(new { success = false, message = "Không tìm thấy reminder" });
            return Ok(new { success = true, data = r });
        }

        public class UpdateReminderRequest { public DateTime? DueDate { get; set; } public int? DueMileage { get; set; } }
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReminderRequest req)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return NotFound(new { success = false, message = "Không tìm thấy reminder" });
            if (req.DueDate.HasValue) r.DueDate = DateOnly.FromDateTime(req.DueDate.Value);
            if (req.DueMileage.HasValue) r.DueMileage = req.DueMileage;
            await _repo.UpdateAsync(r);
            return Ok(new { success = true, data = r });
        }

        [HttpPatch("{id:int}/complete")]
        [Authorize]
        public async Task<IActionResult> Complete(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return NotFound(new { success = false, message = "Không tìm thấy reminder" });
            if (r.IsCompleted) return Ok(new { success = true, data = r });
            r.IsCompleted = true;
            r.CompletedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(r);
            return Ok(new { success = true, data = r });
        }

        public class SnoozeRequest { public int Days { get; set; } = 7; }
        [HttpPatch("{id:int}/snooze")]
        [Authorize]
        public async Task<IActionResult> Snooze(int id, [FromBody] SnoozeRequest req)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return NotFound(new { success = false, message = "Không tìm thấy reminder" });
            if (!r.DueDate.HasValue) return BadRequest(new { success = false, message = "Reminder chưa có DueDate" });
            var days = (req?.Days ?? 0) > 0 ? req.Days : _options.UpcomingDays;
            r.DueDate = r.DueDate.Value.AddDays(days);
            await _repo.UpdateAsync(r);
            return Ok(new { success = true, data = r });
        }

        [HttpGet("upcoming")]
        [Authorize]
        public async Task<IActionResult> Upcoming([FromQuery] int? customerId = null)
        {
            var now = DateTime.UtcNow.Date;
            var until = now.AddDays(_options.UpcomingDays);
            var list = await _repo.QueryAsync(customerId, null, "PENDING", now, until);
            return Ok(new { success = true, config = new { _options.UpcomingDays }, data = list });
        }

        // Appointment reminders for bookings within configured window
        public class AppointmentDispatchRequest { public int? CenterId { get; set; } public DateTime? Date { get; set; } public int? WindowHours { get; set; } }
        [HttpPost("appointments/dispatch")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> DispatchAppointments([FromBody] AppointmentDispatchRequest req)
        {
            var centerId = req?.CenterId;
            var now = DateTime.UtcNow;
            var to = now.AddHours(req?.WindowHours ?? _options.AppointmentReminderHours);
            // Đơn giản: lấy bookings tạo trong khoảng (placeholder vì không có BookingDate trong entity)
            // Có thể thay bằng thực thể/thuộc tính phù hợp nếu sẵn có
            var upcomingBookings = await Task.FromResult(new System.Collections.Generic.List<object>());
            var sent = 0;
            // Placeholder gửi 0 do thiếu BookingDate; giữ endpoint để FE tích hợp sớm
            return Ok(new { success = true, sent, windowHours = (req?.WindowHours ?? _options.AppointmentReminderHours) });
        }

        [HttpPost("{id:int}/send-test")]
        [Authorize]
        public async Task<IActionResult> SendTest(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            if (r == null) return NotFound(new { success = false, message = "Không tìm thấy reminder" });
            var email = r.Vehicle?.Customer?.User?.Email;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var subject = $"Nhắc bảo dưỡng cho xe #{r.VehicleId}";
                var body = $"<p>Xin chào, đây là email nhắc bảo dưỡng dịch vụ #{r.ServiceId} cho xe của bạn.</p>";
                await _email.SendEmailAsync(email, subject, body);
            }
            return Ok(new { success = true });
        }

        // Dispatch reminders by list or auto by config UpcomingDays
        public class DispatchRequest { public int[] ReminderIds { get; set; } public bool Auto { get; set; } = false; public int? UpcomingDays { get; set; } }
        [HttpPost("dispatch")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchRequest req)
        {
            var sent = 0;
            var reminders = new System.Collections.Generic.List<EVServiceCenter.Domain.Entities.MaintenanceReminder>();
            if (req?.Auto == true)
            {
                var now = DateTime.UtcNow.Date;
                var until = now.AddDays(req.UpcomingDays.GetValueOrDefault(_options.UpcomingDays));
                var list = await _repo.QueryAsync(null, null, "PENDING", now, until);
                reminders.AddRange(list);
            }
            else if (req?.ReminderIds != null && req.ReminderIds.Length > 0)
            {
                foreach (var id in req.ReminderIds)
                {
                    var r = await _repo.GetByIdAsync(id);
                    if (r != null) reminders.Add(r);
                }
            }

            foreach (var r in reminders)
            {
                var email = r.Vehicle?.Customer?.User?.Email;
                if (string.IsNullOrWhiteSpace(email)) continue;
                var subject = $"Nhắc bảo dưỡng xe #{r.VehicleId}";
                var body = $"<p>Xin chào, lịch bảo dưỡng của bạn sắp đến hạn.</p>";
                await _email.SendEmailAsync(email, subject, body);
                sent++;
            }
            return Ok(new { success = true, sent });
        }
    }
}


