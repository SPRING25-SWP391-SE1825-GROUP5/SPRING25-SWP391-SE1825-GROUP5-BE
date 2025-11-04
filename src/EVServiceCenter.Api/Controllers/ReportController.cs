using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Interfaces;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/Report")]
    [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
    public class CenterReportController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IWorkOrderPartRepository _workOrderPartRepo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IRevenueReportService _revenueReportService;
        private readonly ITechnicianReportsService _technicianReportsService;

        public CenterReportController(
            IBookingRepository bookingRepo,
            IInvoiceRepository invoiceRepo,
            IPaymentRepository paymentRepo,
            IWorkOrderPartRepository workOrderPartRepo,
            IInventoryRepository inventoryRepo,
            IRevenueReportService revenueReportService,
            ITechnicianReportsService technicianReportsService)
        {
            _bookingRepo = bookingRepo;
            _invoiceRepo = invoiceRepo;
            _paymentRepo = paymentRepo;
            _workOrderPartRepo = workOrderPartRepo;
            _inventoryRepo = inventoryRepo;
            _revenueReportService = revenueReportService;
            _technicianReportsService = technicianReportsService;
        }

        /// <summary>
        /// Lấy tổng doanh thu theo khoảng thời gian với các mode: day/week/month/quarter/year
        /// GET /api/Report/centers/{centerId}/revenue?from=...&to=...&granularity=day|week|month|quarter|year
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="from">Ngày bắt đầu (nullable, mặc định 30 ngày trước)</param>
        /// <param name="to">Ngày kết thúc (nullable, mặc định hôm nay)</param>
        /// <param name="granularity">Chế độ phân loại: day, week, month, quarter, year (mặc định: day)</param>
        /// <returns>Response chứa totalRevenue, granularity, và items (period, revenue)</returns>
        [HttpGet("centers/{centerId}/revenue")]
        public async Task<IActionResult> GetRevenueByPeriod(
            int centerId, 
            [FromQuery] DateTime? from = null, 
            [FromQuery] DateTime? to = null, 
            [FromQuery] string granularity = "day")
        {
            try
            {
                var result = await _revenueReportService.GetRevenueByPeriodAsync(centerId, from, to, granularity);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi lấy doanh thu theo period", error = ex.Message });
            }
        }

        // GET /api/Reporting/centers/{centerId}/bookings/status?from=...&to=...
        [HttpGet("centers/{centerId}/bookings/status")]
        public async Task<IActionResult> GetBookingStatusCounts(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            var bookings = await _bookingRepo.GetBookingsByCenterIdAsync(centerId, 1, 100000, null, start, end);

            var groups = bookings
                .GroupBy(b => (b.Status ?? "").ToUpperInvariant())
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList();
            var total = bookings.Count;
            return Ok(new { success = true, total, items = groups });
        }

        // GET /api/Reporting/centers/{centerId}/parts/top?from=...&to=...&sort=qty|value&limit=10
        [HttpGet("centers/{centerId}/parts/top")]
        public async Task<IActionResult> GetTopParts(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string sort = "qty", [FromQuery] int limit = 10)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            var parts = await _workOrderPartRepo.GetByCenterAndDateRangeAsync(centerId, start, end);

            var grouped = parts
                .GroupBy(p => p.PartId)
                .Select(g => new {
                    partId = g.Key,
                    partName = g.First().Part?.PartName,
                    qty = g.Sum(x => x.QuantityUsed),
                    value = g.Sum(x => (decimal)(x.QuantityUsed * (x.Part?.Price ?? 0m)))
                });

            var ordered = sort == "value" ? grouped.OrderByDescending(x => x.value) : grouped.OrderByDescending(x => x.qty);
            var items = ordered.Take(Math.Max(1, limit)).ToList();
            return Ok(new { success = true, items });
        }

        // GET /api/Reporting/centers/{centerId}/inventory/low-stock?threshold=number
        [HttpGet("centers/{centerId}/inventory/low-stock")]
        public async Task<IActionResult> GetLowStock(int centerId, [FromQuery] int threshold = 5)
        {
            threshold = threshold < 0 ? 0 : threshold;
            var inv = await _inventoryRepo.GetInventoryByCenterIdAsync(centerId);
            if (inv == null) return Ok(new { success = true, items = Array.Empty<object>() });
            var invParts = await _inventoryRepo.GetInventoryPartsByInventoryIdAsync(inv.InventoryId);

            var items = new List<object>();
            foreach (var ip in invParts.Where(x => x.CurrentStock <= threshold))
            {
                var part = await _inventoryRepo.GetPartByIdAsync(ip.PartId);
                items.Add(new { partId = ip.PartId, partName = part?.PartName, currentStock = ip.CurrentStock, minThreshold = ip.MinimumStock });
            }
            return Ok(new { success = true, items });
        }

        /// <summary>
        /// Lấy danh sách doanh thu theo service cho một center, bao gồm cả service không có doanh thu (revenue = 0)
        /// GET /api/Report/centers/{centerId}/revenue-by-service?from=...&to=...
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="from">Ngày bắt đầu (nullable, mặc định 30 ngày trước)</param>
        /// <param name="to">Ngày kết thúc (nullable, mặc định hôm nay)</param>
        /// <returns>Response chứa danh sách service với revenue (bao gồm cả service revenue = 0)</returns>
        [HttpGet("centers/{centerId}/revenue-by-service")]
        public async Task<IActionResult> GetRevenueByService(
            int centerId, 
            [FromQuery] DateTime? from = null, 
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var result = await _revenueReportService.GetRevenueByServiceAsync(centerId, from, to);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi lấy doanh thu theo service", error = ex.Message });
            }
        }

        // GET /api/Reporting/centers/{centerId}/booking-cancellation?from=...&to=...
        [HttpGet("centers/{centerId}/booking-cancellation")]
        public async Task<IActionResult> GetBookingCancellationRate(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            var bookings = await _bookingRepo.GetBookingsByCenterIdAsync(centerId, 1, 100000, null, start, end);

            var cancelled = bookings.Count(b => (b.Status ?? string.Empty).ToUpperInvariant() == "CANCELLED");
            var served = bookings.Count(b => {
                var s = (b.Status ?? string.Empty).ToUpperInvariant();
                return s == "COMPLETED" || s == "PAID";
            });
            var rate = (cancelled + served) > 0 ? (decimal)cancelled / (cancelled + served) : 0m;
            return Ok(new { success = true, cancelled, totalServed = served, cancellationRate = rate });
        }

        // GET /api/Reporting/centers/{centerId}/technicians/productivity?from=...&to=...
        [HttpGet("centers/{centerId}/technicians/productivity")]
        public async Task<IActionResult> GetTechnicianProductivity(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            // Simplified: use completed count per technician
            var bookings = await _bookingRepo.GetBookingsByCenterIdAsync(centerId, 1, 100000, null, start, end);
            var items = bookings
                .Where(b => (b.Status ?? string.Empty).ToUpperInvariant() == "COMPLETED" || (b.Status ?? string.Empty).ToUpperInvariant() == "PAID")
                .GroupBy(b => b.TechnicianSlotId)
                .Select(g => new { technicianSlotId = g.Key, completedCount = g.Count() })
                .OrderByDescending(x => x.completedCount)
                .ToList();

            return Ok(new { success = true, items });
        }

        /// <summary>
        /// Lấy tỉ lệ lấp đầy (utilization rate) của center theo khoảng thời gian
        /// Hỗ trợ groupBy theo day/week/month/quarter/year khi chọn khoảng thời gian dài
        /// GET /api/Report/centers/{centerId}/utilization-rate?from=...&to=...&granularity=day|week|month|quarter|year
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="from">Ngày bắt đầu (nullable, mặc định 30 ngày trước)</param>
        /// <param name="to">Ngày kết thúc (nullable, mặc định hôm nay)</param>
        /// <param name="granularity">Chế độ phân loại: day, week, month, quarter, year (nullable, tự động chọn dựa trên khoảng thời gian)</param>
        /// <returns>Response chứa utilizationRate trung bình và items theo period (nếu có groupBy)</returns>
        [HttpGet("centers/{centerId}/utilization-rate")]
        public async Task<IActionResult> GetCenterUtilizationRate(
            int centerId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? granularity = null)
        {
            try
            {
                var result = await _technicianReportsService.GetCenterUtilizationRateAsync(centerId, from, to, granularity);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi lấy tỉ lệ lấp đầy", error = ex.Message });
            }
        }
    }
}


