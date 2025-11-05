using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Interfaces;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/maintenance-checklist")]
    public class MaintenanceChecklistController : ControllerBase
    {
        private readonly IMaintenanceChecklistRepository _checkRepo;
        private readonly IMaintenanceChecklistResultRepository _resultRepo;
        private readonly IPdfInvoiceService _pdfInvoiceService;
        private readonly IBookingRepository _bookingRepo;
        private readonly IPartRepository _partRepo;
        private readonly IWorkOrderPartRepository _workOrderPartRepo;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<EVServiceCenter.Api.BookingHub> _hub;
        // WorkOrderRepository removed - functionality merged into BookingRepository
        // Removed: IServicePartRepository _servicePartRepo;

        public MaintenanceChecklistController(
            IMaintenanceChecklistRepository checkRepo,
            IMaintenanceChecklistResultRepository resultRepo,
            IPdfInvoiceService pdfInvoiceService,
            IBookingRepository bookingRepo,
            IPartRepository partRepo,
            IWorkOrderPartRepository workOrderPartRepo,
            INotificationService notificationService,
            Microsoft.AspNetCore.SignalR.IHubContext<EVServiceCenter.Api.BookingHub> hub)
        {
            _checkRepo = checkRepo;
            _resultRepo = resultRepo;
            _pdfInvoiceService = pdfInvoiceService;
            _bookingRepo = bookingRepo;
            _partRepo = partRepo;
            _workOrderPartRepo = workOrderPartRepo;
            _notificationService = notificationService;
            _hub = hub;
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
                categoryId = r.CategoryId,
                categoryName = r.Category?.CategoryName,
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



        // Đánh giá theo ResultId (kỹ thuật viên đánh giá từng category trong checklist)
        public class EvaluateResultRequest
        {
            public string Description { get; set; } = string.Empty;
            public string Result { get; set; } = string.Empty;
            public bool RequireReplacement { get; set; } = false;
            public int ReplacementQuantity { get; set; } = 1;
            public int? ReplacementPartId { get; set; }
        }

        // PUT /api/maintenance-checklist/{bookingId}/results/{resultId}
        [HttpPut("{bookingId:int}/results/{resultId:int}")]
        public async Task<IActionResult> EvaluateResult(int bookingId, int resultId, [FromBody] EvaluateResultRequest req)
        {
            // Guard: only allow when booking is IN_PROGRESS
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            var status = (booking.Status ?? string.Empty).ToUpperInvariant();
            if (status == "CANCELED" || status == "CANCELLED" || status == "COMPLETED" || status == "PAID")
                return BadRequest(new { success = false, message = "Không thể đánh giá checklist khi booking đã hoàn tất/hủy" });
            if (status != "IN_PROGRESS")
                return BadRequest(new { success = false, message = "Chỉ có thể đánh giá khi booking ở trạng thái IN_PROGRESS" });

            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            // Tìm result theo ResultId
            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var existing = results.FirstOrDefault(r => r.ResultId == resultId);
            if (existing == null) return NotFound(new { success = false, message = "Checklist result không tồn tại" });

            var normalizedResult = (req?.Result ?? string.Empty).Trim();
            if (string.Equals(normalizedResult, "FAIL", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(req?.Description))
            {
                return BadRequest(new { success = false, message = "Mục đánh giá FAIL bắt buộc phải ghi lý do (description)" });
            }

            var entity = new MaintenanceChecklistResult
            {
                ResultId = resultId,
                ChecklistId = checklist.ChecklistId,
                CategoryId = existing.CategoryId,
                Description = req?.Description ?? string.Empty,
                Result = normalizedResult,
                Status = string.IsNullOrEmpty(req?.Result) || req?.Result == "PENDING" ? "PENDING" : "CHECKED"
            };
            await _resultRepo.UpsertAsync(entity);

            // Nếu FAIL + requireReplacement = true → tự động tạo WorkOrderPart với part cụ thể
            if (string.Equals(normalizedResult, "FAIL", StringComparison.OrdinalIgnoreCase) &&
                req?.RequireReplacement == true &&
                req.ReplacementPartId.HasValue && req.ReplacementPartId.Value > 0)
            {
                var replacementPartId = req.ReplacementPartId.Value;
                var part = await _partRepo.GetPartByIdAsync(replacementPartId);
                if (part != null && part.IsActive)
                {
                    // Validate: replacement part must belong to the same category as the failed checklist item (if CategoryId exists)
                    if (existing.CategoryId.HasValue)
                    {
                        var partsInCategory = await _partRepo.GetPartsByCategoryIdAsync(existing.CategoryId.Value);
                        var isInCategory = partsInCategory.Any(p => p.PartId == replacementPartId);
                        if (!isInCategory)
                        {
                            return BadRequest(new {
                                success = false,
                                message = $"Phụ tùng {replacementPartId} không thuộc category {existing.CategoryId}. Vui lòng chọn phụ tùng trong category '{existing.Category?.CategoryName ?? "(không rõ)"}'."
                            });
                        }
                    }

                    // Nếu ChecklistResult chưa có CategoryId thì suy ra từ part (nếu có mapping)
                    int? derivedCategoryId = existing.CategoryId;
                    if (!derivedCategoryId.HasValue)
                    {
                        derivedCategoryId = await _partRepo.GetFirstCategoryIdForPartAsync(replacementPartId);
                        if (derivedCategoryId.HasValue)
                        {
                            existing.CategoryId = derivedCategoryId.Value;
                            await _resultRepo.UpdateAsync(existing);
                        }
                    }

                    var quantity = req.ReplacementQuantity > 0 ? req.ReplacementQuantity : 1;
                    var existingWorkOrderPart = (await _workOrderPartRepo.GetByBookingIdAsync(bookingId))
                        .FirstOrDefault(wop => wop.PartId == replacementPartId && wop.Status == "PENDING_CUSTOMER_APPROVAL");

                    if (existingWorkOrderPart == null)
                    {
                        var workOrderPart = new WorkOrderPart
                        {
                            BookingId = bookingId,
                            PartId = replacementPartId,
                                CategoryId = derivedCategoryId,
                            QuantityUsed = quantity,
                            Status = "PENDING_CUSTOMER_APPROVAL"
                        };
                        await _workOrderPartRepo.AddAsync(workOrderPart);

                        // Gửi notification cho customer
                        var bookingDetails = await _bookingRepo.GetBookingWithDetailsByIdAsync(bookingId);
                        var customerUserId = bookingDetails?.Customer?.UserId;
                        if (customerUserId.HasValue)
                        {
                            await _notificationService.SendBookingNotificationAsync(
                                customerUserId.Value,
                                "Phụ tùng cần thay thế",
                                $"Phụ tùng {part.PartName ?? $"#{replacementPartId}"} không đạt tiêu chuẩn. Vui lòng xác nhận có đồng ý thay thế không.",
                                "WORKORDER_PART"
                            );
                        }
                    }
                }
            }

            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("checklist.updated", new object[] { new { bookingId } });
            return Ok(new { success = true });
        }

        public class BulkRequest { public System.Collections.Generic.List<BulkItem> Items { get; set; } = new(); public class BulkItem { public int? ResultId { get; set; } public string Description { get; set; } = string.Empty; public string Result { get; set; } = string.Empty; } }

        // PUT /api/maintenance-checklist/{bookingId}
        [HttpPut("{bookingId:int}")]
        public async Task<IActionResult> UpdateBulk(int bookingId, [FromBody] BulkRequest req)
        {
            // Guard: only allow when booking is IN_PROGRESS
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            var status = (booking.Status ?? string.Empty).ToUpperInvariant();
            if (status == "CANCELED" || status == "CANCELLED" || status == "COMPLETED" || status == "PAID")
                return BadRequest(new { success = false, message = "Không thể cập nhật checklist khi booking đã hoàn tất/hủy" });
            if (status != "IN_PROGRESS")
                return BadRequest(new { success = false, message = "Chỉ có thể cập nhật khi booking ở trạng thái IN_PROGRESS" });

            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null) return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            var incoming = (req.Items ?? new System.Collections.Generic.List<BulkRequest.BulkItem>()).ToList();

            var invalidFailItems = incoming
                .Select((i, idx) => new { Item = i, Index = idx })
                .Where(x => string.Equals((x.Item.Result ?? string.Empty).Trim(), "FAIL", StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrWhiteSpace(x.Item.Description))
                .ToList();
            if (invalidFailItems.Any())
            {
                return BadRequest(new {
                    success = false,
                    message = "Các mục FAIL bắt buộc phải ghi lý do (description)",
                    items = invalidFailItems.Select(x => new { index = x.Index, resultId = x.Item.ResultId })
                });
            }

            var existingResults = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);
            var resultsById = existingResults.ToDictionary(r => r.ResultId);

            var results = new System.Collections.Generic.List<MaintenanceChecklistResult>();
            foreach (var i in incoming)
            {
                if (!i.ResultId.HasValue || i.ResultId.Value <= 0)
                {
                    continue;
                }

                if (!resultsById.TryGetValue(i.ResultId.Value, out var existing))
                {
                    continue;
                }

                if (existing.ChecklistId != checklist.ChecklistId)
                {
                    continue;
                }

                var result = new MaintenanceChecklistResult
                {
                    ResultId = existing.ResultId,
                ChecklistId = checklist.ChecklistId,
                    CategoryId = existing.CategoryId,
                Description = i.Description,
                    Result = (i.Result ?? string.Empty).Trim(),
                Status = string.IsNullOrEmpty(i.Result) || i.Result == "PENDING" ? "PENDING" : "CHECKED"
                };
                results.Add(result);
            }

            await _resultRepo.UpsertManyAsync(results);
            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("checklist.updated", new object[] { new { bookingId } });
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
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            var bookingStatus = (booking.Status ?? string.Empty).ToUpperInvariant();
            if (bookingStatus == "CANCELED" || bookingStatus == "CANCELLED")
                return BadRequest(new { success = false, message = "Không thể cập nhật trạng thái checklist khi booking đã hủy" });

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
        [Authorize(Roles = "TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> ExportPdf(int bookingId)
        {
            try
            {
                var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
                if (checklist == null)
                    return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

                // Sử dụng PdfInvoiceService để tạo PDF với thông tin maintenance checklist
                var pdfBytes = await _pdfInvoiceService.GenerateMaintenanceReportPdfAsync(bookingId);

                return File(pdfBytes, "application/pdf", $"MaintenanceChecklist_{bookingId}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi xuất PDF: " + ex.Message });
            }
        }

        public class UpdateStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }

        // PUT /api/maintenance-checklist/{bookingId}/confirm
        [HttpPut("{bookingId:int}/confirm")]
        public async Task<IActionResult> ConfirmCompletion(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null)
                return NotFound(new { success = false, message = "Booking không tồn tại" });
            var bookingStatus = (booking.Status ?? string.Empty).ToUpperInvariant();
            if (bookingStatus == "CANCELED" || bookingStatus == "CANCELLED")
                return BadRequest(new { success = false, message = "Không thể xác nhận checklist khi booking đã hủy" });

            var checklist = await _checkRepo.GetByBookingIdAsync(bookingId);
            if (checklist == null)
                return NotFound(new { success = false, message = "Checklist chưa được khởi tạo" });

            // Lấy tất cả results của checklist
            var results = await _resultRepo.GetByChecklistIdAsync(checklist.ChecklistId);

            if (results == null || !results.Any())
                return BadRequest(new { success = false, message = "Checklist chưa có phụ tùng nào" });

            // Kiểm tra xem tất cả items đã được đánh giá chưa
            var pendingItems = results.Where(r =>
                string.IsNullOrEmpty(r.Result) ||
                string.Equals(r.Result, "PENDING", StringComparison.OrdinalIgnoreCase) ||
                r.Status == "PENDING"
            ).ToList();

            if (pendingItems.Any())
            {
                return BadRequest(new {
                    success = false,
                    message = $"Không thể xác nhận hoàn thành. Còn {pendingItems.Count} phụ tùng chưa được đánh giá",
                    pendingItems = pendingItems.Select(item => new {
                        resultId = item.ResultId,
                        categoryId = item.CategoryId,
                        categoryName = item.Category?.CategoryName ?? item.Description,
                        status = item.Status
                    })
                });
            }

            // Tất cả items đã được đánh giá (PASS hoặc FAIL)
            checklist.Status = "COMPLETED";
            await _checkRepo.UpdateAsync(checklist);

            // Đếm số lượng PASS, FAIL, NA
            var passCount = results.Count(r => string.Equals(r.Result, "PASS", StringComparison.OrdinalIgnoreCase));
            var failCount = results.Count(r => string.Equals(r.Result, "FAIL", StringComparison.OrdinalIgnoreCase));
            var naCount = results.Count(r => string.Equals(r.Result, "NA", StringComparison.OrdinalIgnoreCase));
            var totalCount = results.Count;

            await _hub.Clients.Group($"booking:{bookingId}").SendCoreAsync("checklist.updated", new object[] { new { bookingId, status = checklist.Status } });
            return Ok(new {
                success = true,
                message = "Xác nhận hoàn thành checklist thành công",
                checklistId = checklist.ChecklistId,
                status = checklist.Status,
                statistics = new {
                    total = totalCount,
                    pass = passCount,
                    fail = failCount,
                    na = naCount
                }
            });
        }
    }
}


