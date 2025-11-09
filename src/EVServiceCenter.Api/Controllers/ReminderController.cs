using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.Extensions.Options;
using System.Linq;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/reminders")]
    public class ReminderController : ControllerBase
    {
        private readonly IMaintenanceReminderRepository _repo;
        private readonly IEmailService _email;
        private readonly MaintenanceReminderOptions _options;
        private readonly IBookingRepository _bookingRepository;
        private readonly INotificationService _notificationService;
        private readonly IEmailTemplateRenderer _templateRenderer;

        public ReminderController(IMaintenanceReminderRepository repo, IEmailService email, IOptions<MaintenanceReminderOptions> options, IBookingRepository bookingRepository, INotificationService notificationService, IEmailTemplateRenderer templateRenderer)
        {
            _repo = repo;
            _email = email;
            _options = options.Value;
            _bookingRepository = bookingRepository;
            _notificationService = notificationService;
            _templateRenderer = templateRenderer;
        }


        [HttpPost("vehicles/{vehicleId:int}/set")]
        [Authorize]
        public async Task<IActionResult> SetVehicleReminders(int vehicleId, [FromBody] SetVehicleRemindersRequest req)
        {
            if (vehicleId <= 0) return BadRequest(new { success = false, message = "vehicleId không hợp lệ" });
            var items = req?.Items ?? new System.Collections.Generic.List<SetVehicleReminderItem>();
            if (items.Count == 0) return BadRequest(new { success = false, message = "Danh sách reminders trống" });

            var created = new System.Collections.Generic.List<EVServiceCenter.Domain.Entities.MaintenanceReminder>();
            foreach (var it in items)
            {
                var entity = new EVServiceCenter.Domain.Entities.MaintenanceReminder
                {
                    VehicleId = vehicleId,
                    ServiceId = it.ServiceId,
                    DueDate = it.DueDate.HasValue ? DateOnly.FromDateTime(it.DueDate.Value) : null,
                    DueMileage = it.DueMileage,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    Type = it.Type ?? EVServiceCenter.Domain.Enums.ReminderType.MAINTENANCE,
                    Status = EVServiceCenter.Domain.Enums.ReminderStatus.PENDING,
                    CadenceDays = it.CadenceDays,
                    UpdatedAt = DateTime.UtcNow
                };
                var r = await _repo.CreateAsync(entity);
                created.Add(r);
            }

            return Ok(new { success = true, vehicleId, added = created.Count, data = created.Select(x => new { x.ReminderId, x.ServiceId, x.DueDate, x.DueMileage }) });
        }

        // Get alert reminders (upcoming/past-due) for a specific vehicle
        [HttpGet("vehicles/{vehicleId:int}/alerts")]
        [Authorize]
        public async Task<IActionResult> GetVehicleAlerts(int vehicleId)
        {
            if (vehicleId <= 0) return BadRequest(new { success = false, message = "vehicleId không hợp lệ" });
            var now = DateTime.UtcNow.Date;
            var until = now.AddDays(_options.UpcomingDays);
            // Lấy tất cả reminders PENDING của vehicle, rồi lọc theo DueDate đến hạn trong cửa sổ cảnh báo
            var pending = await _repo.QueryAsync(null, vehicleId, "PENDING", null, null);
            var alerts = pending
                .Where(r => r.DueDate.HasValue && r.DueDate.Value.ToDateTime(TimeOnly.MinValue) <= until)
                .OrderBy(r => r.DueDate)
                .ToList();

            return Ok(new { success = true, config = new { _options.UpcomingDays }, vehicleId, count = alerts.Count, data = alerts });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List([FromQuery] int? customerId = null, [FromQuery] int? vehicleId = null, [FromQuery] string? status = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var items = await _repo.QueryAsync(customerId, vehicleId, status ?? string.Empty, from, to);
            return Ok(new { success = true, data = items });
        }

        public class CreateReminderRequest { public int VehicleId { get; set; } public int? ServiceId { get; set; } public DateTime? DueDate { get; set; } public int? DueMileage { get; set; } public int? CadenceDays { get; set; } public EVServiceCenter.Domain.Enums.ReminderType? Type { get; set; } }
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
                CreatedAt = DateTime.UtcNow,
                Type = req.Type ?? EVServiceCenter.Domain.Enums.ReminderType.MAINTENANCE,
                Status = EVServiceCenter.Domain.Enums.ReminderStatus.PENDING,
                CadenceDays = req.CadenceDays,
                UpdatedAt = DateTime.UtcNow
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
            var days = (req?.Days ?? 0) > 0 ? (req?.Days ?? 0) : _options.UpcomingDays;
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
            var now = DateTime.UtcNow;
            var baseDate = req?.Date ?? now;
            var windowHours = req?.WindowHours ?? _options.AppointmentReminderHours;
            var windowEnd = baseDate.AddHours(windowHours);

            var bookings = req?.CenterId.HasValue == true
                ? await _bookingRepository.GetBookingsByCenterIdAsync(req.CenterId.Value, 1, int.MaxValue, "CONFIRMED", null, null, "createdAt", "desc")
                : await _bookingRepository.GetAllBookingsAsync();

            var candidates = bookings
                .Where(b => (b.Status == "CONFIRMED" || b.Status == "IN_PROGRESS") && b.TechnicianSlotId.HasValue)
                .ToList();

            var detailed = new System.Collections.Generic.List<EVServiceCenter.Domain.Entities.Booking>();
            foreach (var b in candidates)
            {
                var full = await _bookingRepository.GetBookingDetailAsync(b.BookingId);
                if (full?.TechnicianTimeSlot?.Slot != null)
                {
                    detailed.Add(full);
                }
            }

            var withinWindow = detailed
                .Select(b => new
                {
                    Booking = b,
                    At = new DateTime(b.TechnicianTimeSlot!.WorkDate.Year, b.TechnicianTimeSlot.WorkDate.Month, b.TechnicianTimeSlot.WorkDate.Day,
                                      b.TechnicianTimeSlot.Slot.SlotTime.Hour, b.TechnicianTimeSlot.Slot.SlotTime.Minute, b.TechnicianTimeSlot.Slot.SlotTime.Second, DateTimeKind.Utc)
                })
                .Where(x => x.At >= now && x.At <= windowEnd)
                .ToList();

            var sent = 0;
            foreach (var x in withinWindow)
            {
                var email = x.Booking.Customer?.User?.Email;
                if (string.IsNullOrWhiteSpace(email)) continue;
                var subject = "Nhắc lịch hẹn";
                var body = await _templateRenderer.RenderAsync("AppointmentReminder", new System.Collections.Generic.Dictionary<string, string>
                {
                    ["bookingId"] = x.Booking.BookingId.ToString(),
                    ["centerName"] = x.Booking.Center?.CenterName ?? string.Empty,
                    ["appointmentUtc"] = x.At.ToString("u")
                });
                await _email.SendEmailAsync(email, subject, body);
                var userId = x.Booking.Customer?.User?.UserId ?? 0;
                if (userId > 0)
                {
                    await _notificationService.SendBookingNotificationAsync(userId, subject, $"Booking #{x.Booking.BookingId} lúc {x.At:u}", "APPOINTMENT");
                }
                sent++;
            }

            return Ok(new { success = true, sent, windowHours });
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
        public class DispatchRequest { public int[] ReminderIds { get; set; } = Array.Empty<int>(); public bool Auto { get; set; } = false; public int? UpcomingDays { get; set; } }
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
                var subject = "Nhắc bảo dưỡng";
                var body = await _templateRenderer.RenderAsync("MaintenanceReminder", new System.Collections.Generic.Dictionary<string, string>
                {
                    ["vehicleId"] = r.VehicleId.ToString(),
                    ["serviceId"] = (r.ServiceId?.ToString() ?? string.Empty),
                    ["dueDate"] = (r.DueDate?.ToDateTime(TimeOnly.MinValue).ToString("u") ?? string.Empty),
                    ["dueMileage"] = (r.DueMileage?.ToString() ?? string.Empty)
                });
                await _email.SendEmailAsync(email, subject, body);
                var userId = r.Vehicle?.Customer?.User?.UserId ?? 0;
                if (userId > 0)
                {
                    await _notificationService.SendBookingNotificationAsync(userId, subject, $"Xe #{r.VehicleId} đến hạn bảo dưỡng", "MAINTENANCE");
                }
                sent++;
            }
            return Ok(new { success = true, sent });
        }

        // Admin endpoints
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> ListForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? customerId = null,
            [FromQuery] int? vehicleId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string sortBy = "createdAt",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(new { success = false, message = "Page phải lớn hơn 0" });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { success = false, message = "Page size phải từ 1 đến 100" });
                }

                var (items, totalCount) = await _repo.QueryForAdminAsync(
                    page, pageSize, customerId, vehicleId, status, type, from, to, searchTerm, sortBy, sortOrder);

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                return Ok(new { success = true, data = items, pagination });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách reminders: {ex.Message}" });
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                // Get all reminders for statistics
                var allReminders = await _repo.QueryAsync(null, null, null, null, null);

                var stats = new
                {
                    Total = allReminders.Count,
                    Pending = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.PENDING && !r.IsCompleted),
                    Due = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.DUE),
                    Overdue = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.OVERDUE),
                    Completed = allReminders.Count(r => r.IsCompleted || r.Status == Domain.Enums.ReminderStatus.COMPLETED),
                    ByType = new
                    {
                        Maintenance = allReminders.Count(r => r.Type == Domain.Enums.ReminderType.MAINTENANCE),
                        Package = allReminders.Count(r => r.Type == Domain.Enums.ReminderType.PACKAGE),
                        Appointment = allReminders.Count(r => r.Type == Domain.Enums.ReminderType.APPOINTMENT)
                    },
                    ByStatus = new
                    {
                        Pending = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.PENDING),
                        Due = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.DUE),
                        Overdue = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.OVERDUE),
                        Completed = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.COMPLETED),
                        Expired = allReminders.Count(r => r.Status == Domain.Enums.ReminderStatus.EXPIRED)
                    }
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thống kê: {ex.Message}" });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var reminder = await _repo.GetByIdAsync(id);
                if (reminder == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy reminder" });
                }

                await _repo.DeleteAsync(id);
                return Ok(new { success = true, message = "Đã xóa reminder thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa reminder: {ex.Message}" });
            }
        }
    }
}


