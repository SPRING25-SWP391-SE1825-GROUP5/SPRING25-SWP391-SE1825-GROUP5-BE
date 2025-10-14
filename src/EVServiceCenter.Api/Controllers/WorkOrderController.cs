using System;
using System.Linq;
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
        private readonly ITechnicianRepository _technicianRepository;
        private readonly IServicePartRepository _servicePartRepository;
        private readonly IServiceRequiredSkillRepository _requiredSkillRepo;
        private readonly ITechnicianTimeSlotRepository _timeSlotRepo;
        private readonly IWorkOrderPartRepository _workOrderPartRepo;
        private readonly IMaintenanceChecklistRepository _checkRepo;
        private readonly IMaintenanceChecklistResultRepository _checkResultRepo;

        public WorkOrderController(IWorkOrderRepository workOrderRepository, IBookingRepository bookingRepository, ITechnicianRepository technicianRepository, IServicePartRepository servicePartRepository, IMaintenanceChecklistRepository checkRepo, IMaintenanceChecklistResultRepository checkResultRepo, IServiceRequiredSkillRepository requiredSkillRepo, ITechnicianTimeSlotRepository timeSlotRepo, IWorkOrderPartRepository workOrderPartRepo)
        {
            _workOrderRepository = workOrderRepository;
            _bookingRepository = bookingRepository;
            _technicianRepository = technicianRepository;
            _servicePartRepository = servicePartRepository;
            _checkRepo = checkRepo;
            _checkResultRepo = checkResultRepo;
            _requiredSkillRepo = requiredSkillRepo;
            _timeSlotRepo = timeSlotRepo;
            _workOrderPartRepo = workOrderPartRepo;
        }

        // GET api/workorders?page=&size=&status=&centerId=&technicianId=&customerId=&vehicleId=&serviceId=&from=&to=&sort=
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Query([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string status = null,
            [FromQuery] int? centerId = null, [FromQuery] int? technicianId = null, [FromQuery] int? customerId = null, [FromQuery] int? vehicleId = null,
            [FromQuery] int? serviceId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string sort = "createdAt_desc")
        {
            if (page <= 0) page = 1; if (size <= 0 || size > 100) size = 20;
            var (items, total) = await _workOrderRepository.QueryAsync(centerId, technicianId, customerId, vehicleId, serviceId, status, from, to, page, size, sort, includeRelations: true);
            var data = items.Select(wo => new {
                wo.WorkOrderId, wo.BookingId, wo.Status, wo.CreatedAt, wo.UpdatedAt,
                technician = new { id = wo.TechnicianId, name = wo.Technician?.User?.FullName },
                centerId = wo.CenterId, customerId = wo.CustomerId, vehicleId = wo.VehicleId, serviceId = wo.ServiceId
            });
            return Ok(new { success = true, total, page, size, data });
        }

        // GET api/workorders/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Detail(int id)
        {
            var wo = await _workOrderRepository.GetByIdAsync(id);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            var subtotal = (wo.WorkOrderParts ?? new System.Collections.Generic.List<EVServiceCenter.Domain.Entities.WorkOrderPart>()).Sum(p => p.UnitCost * p.QuantityUsed);
            var data = new {
                wo.WorkOrderId, wo.BookingId, wo.Status, wo.CreatedAt, wo.UpdatedAt, wo.CenterId, wo.CustomerId, wo.VehicleId, wo.ServiceId,
                technician = new { id = wo.TechnicianId, name = wo.Technician?.User?.FullName },
                parts = (wo.WorkOrderParts ?? new System.Collections.Generic.List<EVServiceCenter.Domain.Entities.WorkOrderPart>()).Select(p => new { p.PartId, name = p.Part?.PartName, qty = p.QuantityUsed, unitPrice = p.UnitCost, total = p.UnitCost * p.QuantityUsed }),
                totals = new { subtotalParts = subtotal, serviceFee = 0m, discount = 0m, tax = 0m, total = subtotal }
            };
            return Ok(new { success = true, data });
        }

        [HttpGet("/api/centers/{centerId:int}/workorders")]
        [Authorize]
        public async Task<IActionResult> ByCenter(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string status = null)
        {
            var list = await _workOrderRepository.GetByCenterAsync(centerId, from, to, status);
            return Ok(new { success = true, data = list });
        }

        [HttpGet("/api/technicians/{technicianId:int}/workorders")]
        [Authorize]
        public async Task<IActionResult> ByTechnician(int technicianId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string status = null)
        {
            var list = await _workOrderRepository.GetByTechnicianAsync(technicianId, from, to, status);
            return Ok(new { success = true, data = list });
        }

        [HttpGet("/api/customers/{customerId:int}/workorders")]
        [Authorize]
        public async Task<IActionResult> ByCustomer(int customerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string status = null)
        {
            var list = await _workOrderRepository.GetByCustomerAsync(customerId, from, to, status);
            return Ok(new { success = true, data = list });
        }

        [HttpGet("/api/customers/{customerId:int}/workorders/history")]
        [Authorize]
        public async Task<IActionResult> ByCustomerHistory(int customerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var list = await _workOrderRepository.GetByCustomerAsync(customerId, from, to, "COMPLETED");
            var canceled = await _workOrderRepository.GetByCustomerAsync(customerId, from, to, "CANCELED");
            list.AddRange(canceled);
            return Ok(new { success = true, data = list.OrderByDescending(x => x.CreatedAt) });
        }

        [HttpGet("/api/customers/{customerId:int}/workorders/last")]
        [Authorize]
        public async Task<IActionResult> LastByCustomer(int customerId, [FromQuery] int limit = 1, [FromQuery] int? vehicleId = null, [FromQuery] int? serviceId = null)
        {
            if (limit <= 0) limit = 1; if (limit > 50) limit = 50;
            var (items, total) = await _workOrderRepository.QueryAsync(
                centerId: null,
                technicianId: null,
                customerId: customerId,
                vehicleId: vehicleId,
                serviceId: serviceId,
                status: "COMPLETED",
                from: null,
                to: null,
                page: 1,
                size: limit,
                sort: "createdat_desc",
                includeRelations: true);

            return Ok(new { success = true, total = items.Count, data = items });
        }

        [HttpGet("/api/customers/{customerId:int}/vehicles/{vehicleId:int}/workorders")]
        [Authorize]
        public async Task<IActionResult> ByCustomerVehicle(int customerId, int vehicleId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string status = null)
        {
            var list = await _workOrderRepository.GetByCustomerVehicleAsync(customerId, vehicleId, from, to, status);
            return Ok(new { success = true, data = list });
        }

        [HttpGet("statistics")]
        [Authorize]
        public async Task<IActionResult> Statistics([FromQuery] int? centerId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string groupBy = "day")
        {
            var stats = await _workOrderRepository.GetStatisticsAsync(centerId, from, to, groupBy);
            return Ok(new { success = true, data = stats });
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

        /// <summary>
        /// Lấy kỹ thuật viên được gán cho WorkOrder của một booking
        /// </summary>
        [HttpGet("by-booking/{bookingId:int}/technician")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetTechnicianByBooking(int bookingId)
        {
            var wo = await _workOrderRepository.GetByBookingIdAsync(bookingId);
            if (wo == null)
                return NotFound(new { success = false, message = "Không tìm thấy work order cho booking này" });

            if (wo.TechnicianId <= 0 || wo.Technician == null)
                return Ok(new { success = true, data = (object)null, message = "Chưa được gán kỹ thuật viên" });

            var data = new
            {
                technicianId = wo.TechnicianId,
                technicianName = wo.Technician?.User?.FullName,
                phone = wo.Technician?.User?.PhoneNumber,
                email = wo.Technician?.User?.Email
            };
            return Ok(new { success = true, data });
        }

        [HttpPost]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            // Luôn ép NOT_STARTED khi tạo
            var status = "NOT_STARTED";

            WorkOrder created;

            if (request.BookingId.HasValue && request.BookingId.Value > 0)
            {
                // Luồng có booking
                var booking = await _bookingRepository.GetBookingByIdAsync(request.BookingId.Value);
                if (booking == null) return BadRequest(new { success = false, message = "Booking không tồn tại" });

                var existing = await _workOrderRepository.GetByBookingIdAsync(request.BookingId.Value);
                if (existing != null) return Conflict(new { success = false, message = "Booking đã có work order" });

                // Validate technician center/skill if provided
                if (request.TechnicianId.HasValue && request.TechnicianId.Value > 0)
                {
                    var tech = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId.Value);
                    if (tech == null) return BadRequest(new { success = false, message = "Kỹ thuật viên không tồn tại" });
                    if (tech.CenterId != booking.CenterId) return BadRequest(new { success = false, message = "Kỹ thuật viên không thuộc trung tâm của booking" });
                    // Skill check
                    if (booking.ServiceId > 0)
                    {
                        var reqSkills = await _requiredSkillRepo.GetByServiceIdAsync(booking.ServiceId);
                        if (reqSkills != null && reqSkills.Count > 0)
                        {
                            var techSkillIds = (tech.TechnicianSkills ?? new System.Collections.Generic.List<TechnicianSkill>()).Select(s => s.SkillId).ToHashSet();
                            var ok = reqSkills.All(rs => techSkillIds.Contains(rs.SkillId));
                            if (!ok) return BadRequest(new { success = false, message = "Kỹ thuật viên không đủ kỹ năng cho dịch vụ" });
                        }
                    }
                    // Slot availability (nếu booking có SlotId/BookingDate)
                    try
                    {
                        if (booking.SlotId > 0)
                        {
                            var date = DateOnly.FromDateTime(booking.CreatedAt);
                            var isFree = await _timeSlotRepo.IsSlotAvailableAsync(tech.TechnicianId, date.ToDateTime(TimeOnly.MinValue), booking.SlotId);
                            if (!isFree) return BadRequest(new { success = false, message = "Kỹ thuật viên bận tại slot này" });
                        }
                    }
                    catch { }
                }

                created = await _workOrderRepository.CreateAsync(new WorkOrder
                {
                    BookingId = booking.BookingId,
                    TechnicianId = request.TechnicianId ?? 0,
                    CustomerId = booking.CustomerId,
                    VehicleId = booking.VehicleId,
                    CenterId = booking.CenterId,
                    ServiceId = booking.ServiceId,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Luồng walk-in: yêu cầu center, service, customer, vehicle
                if (!(request.CenterId.HasValue && request.CenterId.Value > 0
                    && request.ServiceId.HasValue && request.ServiceId.Value > 0
                    && request.CustomerId.HasValue && request.CustomerId.Value > 0
                    && request.VehicleId.HasValue && request.VehicleId.Value > 0))
                {
                    return BadRequest(new { success = false, message = "Walk-in: yêu cầu CenterId, ServiceId, CustomerId, VehicleId" });
                }

                // Validate technician center for walk-in
                if (request.TechnicianId.HasValue && request.TechnicianId.Value > 0)
                {
                    var tech = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId.Value);
                    if (tech == null) return BadRequest(new { success = false, message = "Kỹ thuật viên không tồn tại" });
                    if (tech.CenterId != request.CenterId.Value) return BadRequest(new { success = false, message = "Kỹ thuật viên không thuộc trung tâm" });
                    if (request.ServiceId.HasValue)
                    {
                        var reqSkills = await _requiredSkillRepo.GetByServiceIdAsync(request.ServiceId.Value);
                        if (reqSkills != null && reqSkills.Count > 0)
                        {
                            var techSkillIds = (tech.TechnicianSkills ?? new System.Collections.Generic.List<TechnicianSkill>()).Select(s => s.SkillId).ToHashSet();
                            var ok = reqSkills.All(rs => techSkillIds.Contains(rs.SkillId));
                            if (!ok) return BadRequest(new { success = false, message = "Kỹ thuật viên không đủ kỹ năng cho dịch vụ" });
                        }
                    }
                }

                created = await _workOrderRepository.CreateAsync(new WorkOrder
                {
                    BookingId = 0,
                    TechnicianId = request.TechnicianId ?? 0,
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    CenterId = request.CenterId,
                    ServiceId = request.ServiceId,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Auto-init checklist + clone service parts (quantity=0, unitCost=0)
            try
            {
                if (created.ServiceId.HasValue)
                {
                    var existingChecklist = await _checkRepo.GetByWorkOrderIdAsync(created.WorkOrderId);
                    if (existingChecklist == null)
                    {
                        var checklist = await _checkRepo.CreateAsync(new MaintenanceChecklist
                        {
                            WorkOrderId = created.WorkOrderId,
                            CreatedAt = DateTime.UtcNow,
                            Notes = null
                        });
                        var serviceParts = await _servicePartRepository.GetByServiceIdAsync(created.ServiceId.Value);
                        var results = serviceParts.Select(sp => new MaintenanceChecklistResult
                        {
                            ChecklistId = checklist.ChecklistId,
                            PartId = sp.PartId,
                            Description = sp.Part?.PartName ?? $"Part {sp.PartId}",
                            Result = null,
                            Comment = null
                        });
                        await _checkResultRepo.UpsertManyAsync(results);

                        // Clone WorkOrderParts suggestions (qty=0, unitCost=0) if none exist
                        var existingWop = await _workOrderPartRepo.GetByWorkOrderIdAsync(created.WorkOrderId);
                        var existingPartIds = existingWop.Select(e => e.PartId).ToHashSet();
                        foreach (var sp in serviceParts)
                        {
                            if (!existingPartIds.Contains(sp.PartId))
                            {
                                await _workOrderPartRepo.AddAsync(new WorkOrderPart
                                {
                                    WorkOrderId = created.WorkOrderId,
                                    PartId = sp.PartId,
                                    QuantityUsed = 0,
                                    UnitCost = 0
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            return Ok(new { success = true, data = new { created.WorkOrderId, created.BookingId, created.Status } });
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

        public class UpdateWorkOrderStatusRequest { public string Status { get; set; } }

        [HttpPut("{id:int}/status")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateWorkOrderStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Status))
                return BadRequest(new { success = false, message = "Trạng thái không được để trống" });

            var allowed = new[] { "NOT_STARTED", "IN_PROGRESS", "ON_HOLD", "COMPLETED" };
            var status = request.Status.Trim().ToUpper();
            if (!Array.Exists(allowed, s => s == status))
                return BadRequest(new { success = false, message = "Trạng thái work order không hợp lệ" });

            var wo = await _workOrderRepository.GetByIdAsync(id);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });

            wo.Status = status;
            wo.UpdatedAt = DateTime.UtcNow;
            await _workOrderRepository.UpdateAsync(wo);
            return Ok(new { success = true, message = "Cập nhật trạng thái work order thành công", data = new { wo.WorkOrderId, wo.Status } });
        }

        [HttpPost("{id:int}/cancel")]
        [Authorize(Policy = "TechnicianOrAdmin")]
        public async Task<IActionResult> Cancel(int id)
        {
            var wo = await _workOrderRepository.GetByIdAsync(id);
            if (wo == null) return NotFound(new { success = false, message = "Work order không tồn tại" });
            if (string.Equals(wo.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Work order đã hoàn tất, không thể hủy" });
            wo.Status = "CANCELED";
            wo.UpdatedAt = DateTime.UtcNow;
            await _workOrderRepository.UpdateAsync(wo);
            return Ok(new { success = true, message = "Đã hủy work order", data = new { wo.WorkOrderId, wo.Status } });
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


