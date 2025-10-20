using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/maintenance-checklist")]
    public class MaintenanceChecklistController : ControllerBase
    {
        private readonly IMaintenanceChecklistRepository _checkRepo;
        private readonly IMaintenanceChecklistResultRepository _resultRepo;
        // WorkOrderRepository removed - functionality merged into BookingRepository
        // Removed: IServicePartRepository _servicePartRepo;

        public MaintenanceChecklistController(
            IMaintenanceChecklistRepository checkRepo,
            IMaintenanceChecklistResultRepository resultRepo)
        {
            _checkRepo = checkRepo;
            _resultRepo = resultRepo;
            // WorkOrderRepository removed - functionality merged into BookingRepository
        }

        // POST /api/maintenance-checklist/{bookingId}/init
        [HttpPost("{bookingId:int}/init")]
        public async Task<IActionResult> Init(int bookingId)
        {
            // WorkOrder functionality merged into Booking - validate booking exists
            // This would need to be implemented in BookingRepository if needed
            // For now, assume booking exists

            var existing = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (existing != null) return Ok(new { success = true, checklistId = existing.ChecklistId });

            var checklist = await _checkRepo.CreateAsync(new MaintenanceChecklist
            {
                BookingId = bookingId,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                Notes = null
            });

            // Template-based flow: results sẽ được sinh từ ServiceChecklistTemplateItems ở bước riêng

            return Ok(new { success = true, checklistId = checklist.ChecklistId });
        }

        // GET /api/maintenance-checklist/{bookingId}
        [HttpGet("{bookingId:int}")]
        public async Task<IActionResult> Get(int bookingId)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var data = results.Select(r => new
            {
                resultId = r.ResultId,
                partId = r.PartId,
                partName = r.Part?.PartName,
                description = r.Description,
                result = r.Result,
                status = r.Status
            });
            return Ok(new { 
                success = true, 
                checklistId = checklist.ChecklistId, 
                status = checklist.Status,
                items = data 
            });
        }

        public class UpdateItemRequest { public string Description { get; set; } = string.Empty; public string Result { get; set; } = string.Empty; }

        // PUT /api/maintenance-checklist/{bookingId}/{resultId}
        [HttpPut("{bookingId:int}/{resultId:int}")]
        public async Task<IActionResult> UpdateItem(int bookingId, int resultId, [FromBody] UpdateItemRequest req)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            await _resultRepo.UpsertAsync(new MaintenanceChecklistResult
            {
                ResultId = resultId,
                ChecklistId = checklist.ChecklistId,
                Description = req.Description,
                Result = req.Result,
                Status = string.IsNullOrEmpty(req.Result) || req.Result == "PENDING" ? "PENDING" : "CHECKED"
            });
            return Ok(new { success = true });
        }

        // Đánh giá theo PartID (đúng với yêu cầu: kỹ thuật viên đánh giá từng phụ tùng)
        public class EvaluatePartRequest { public string Description { get; set; } = string.Empty; public string Result { get; set; } = string.Empty; }

        // PUT /api/maintenance-checklist/{bookingId}/parts/{partId}
        [HttpPut("{bookingId:int}/parts/{partId:int}")]
        public async Task<IActionResult> EvaluatePart(int bookingId, int partId, [FromBody] EvaluatePartRequest req)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            // Tìm result theo PartID trong checklist này
            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var existing = results.FirstOrDefault(r => r.PartId == partId);

            var entity = new MaintenanceChecklistResult
            {
                ResultId = existing?.ResultId ?? 0,
                ChecklistId = checklist.ChecklistId,
                PartId = partId,
                Description = req.Description,
                Result = req.Result,
                Status = string.IsNullOrEmpty(req.Result) || req.Result == "PENDING" ? "PENDING" : "CHECKED"
            };
            await _resultRepo.UpsertAsync(entity);
            return Ok(new { success = true });
        }

        public class BulkRequest { public System.Collections.Generic.List<BulkItem> Items { get; set; } = new(); public class BulkItem { public int? ResultId { get; set; } public string Description { get; set; } = string.Empty; public string Result { get; set; } = string.Empty; } }

        // PUT /api/maintenance-checklist/{bookingId}
        [HttpPut("{bookingId:int}")]
        public async Task<IActionResult> UpdateBulk(int bookingId, [FromBody] BulkRequest req)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var incoming = (req.Items ?? new System.Collections.Generic.List<BulkRequest.BulkItem>()).ToList();
            var results = incoming.Select(i => new MaintenanceChecklistResult
            {
                ResultId = i.ResultId ?? 0,
                ChecklistId = checklist.ChecklistId,
                Description = i.Description,
                Result = i.Result,
                Status = string.IsNullOrEmpty(i.Result) || i.Result == "PENDING" ? "PENDING" : "CHECKED"
            });
            await _resultRepo.UpsertManyAsync(results);
            return Ok(new { success = true });
        }

        // GET /api/maintenance-checklist/{bookingId}/summary
        [HttpGet("{bookingId:int}/summary")]
        public async Task<IActionResult> Summary(int bookingId)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var pass = results.Count(r => string.Equals(r.Result, "PASS", StringComparison.OrdinalIgnoreCase));
            var fail = results.Count(r => string.Equals(r.Result, "FAIL", StringComparison.OrdinalIgnoreCase));
            var na = results.Count(r => string.Equals(r.Result, "NA", StringComparison.OrdinalIgnoreCase));
            var total = results.Count;
            return Ok(new { success = true, checklistId = checklist.ChecklistId, total, pass, fail, na });
        }

        // PUT /api/maintenance-checklist/{bookingId}/status
        [HttpPut("{bookingId:int}/status")]
        public async Task<IActionResult> UpdateStatus(int bookingId, [FromBody] UpdateStatusRequest req)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            // Validate status
            if (!new[] { "PENDING", "IN_PROGRESS", "COMPLETED" }.Contains(req.Status))
                return BadRequest(new { success = false, message = "Status không hợp lệ. Chỉ chấp nhận: PENDING, IN_PROGRESS, COMPLETED" });

            // Nếu chuyển sang COMPLETED, kiểm tra xem đã đánh giá hết chưa
            if (req.Status == "COMPLETED")
            {
                var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
                var pendingCount = results.Count(r => string.Equals(r.Result, "PENDING", StringComparison.OrdinalIgnoreCase));
                if (pendingCount > 0)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Không thể hoàn thành checklist. Còn {pendingCount} phụ tùng chưa được đánh giá (PENDING)" 
                    });
                }
            }

            checklist.Status = req.Status;
            await _checkRepo.UpdateAsync(checklist);
            
            return Ok(new { success = true, message = $"Đã cập nhật status thành {req.Status}" });
        }

        // GET /api/maintenance-checklist/{bookingId}/status
        [HttpGet("{bookingId:int}/status")]
        public async Task<IActionResult> GetStatus(int bookingId)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var pendingCount = results.Count(r => string.Equals(r.Result, "PENDING", StringComparison.OrdinalIgnoreCase));
            var totalCount = results.Count();

            return Ok(new { 
                success = true, 
                checklistId = checklist.ChecklistId,
                status = checklist.Status,
                totalParts = totalCount,
                pendingParts = pendingCount,
                canComplete = pendingCount == 0 && checklist.Status != "COMPLETED"
            });
        }

        // GET /api/maintenance-checklist/{bookingId}/export
        [HttpGet("{bookingId:int}/export")]
        public async Task<IActionResult> ExportPdf(int bookingId)
        {
            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Checklist #{checklist.ChecklistId} for Booking #{bookingId}");
            foreach (var r in results)
            {
                sb.AppendLine($"- {r.Description}: {r.Result ?? "N/A"} {(string.IsNullOrWhiteSpace(r.Comment) ? string.Empty : "(" + r.Comment + ")")}");
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "application/octet-stream", $"Checklist_{bookingId}.txt");
        }

        public class UpdateStatusRequest 
        { 
            public string Status { get; set; } = string.Empty; 
        }
    }
}


