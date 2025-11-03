using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;

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

        public CenterReportController(
            IBookingRepository bookingRepo,
            IInvoiceRepository invoiceRepo,
            IPaymentRepository paymentRepo,
            IWorkOrderPartRepository workOrderPartRepo,
            IInventoryRepository inventoryRepo)
        {
            _bookingRepo = bookingRepo;
            _invoiceRepo = invoiceRepo;
            _paymentRepo = paymentRepo;
            _workOrderPartRepo = workOrderPartRepo;
            _inventoryRepo = inventoryRepo;
        }

        // GET /api/Reporting/centers/{centerId}/revenue?from=...&to=...&granularity=day|month
        [HttpGet("centers/{centerId}/revenue")]
        public async Task<IActionResult> GetRevenueByPeriod(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string granularity = "day")
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var bookings = await _bookingRepo.GetBookingsByCenterIdAsync(centerId, 1, 100000, null, start, end);
            var revenueByKey = new Dictionary<string, decimal>();
            decimal total = 0m;

            foreach (var b in bookings)
            {
                var invoice = await _invoiceRepo.GetByBookingIdAsync(b.BookingId);
                if (invoice == null) continue;
                var payments = await _paymentRepo.GetByInvoiceIdAsync(invoice.InvoiceId, status: "COMPLETED", method: null, from: start, to: end);
                var amount = payments.Sum(p => (decimal)p.Amount);
                if (amount <= 0) continue;

                var key = granularity == "month" ? new DateTime(b.CreatedAt.Year, b.CreatedAt.Month, 1).ToString("yyyy-MM") : b.CreatedAt.ToString("yyyy-MM-dd");
                if (!revenueByKey.ContainsKey(key)) revenueByKey[key] = 0m;
                revenueByKey[key] += amount;
                total += amount;
            }

            var items = revenueByKey.OrderBy(kv => kv.Key).Select(kv => new { period = kv.Key, revenue = kv.Value }).ToList();
            return Ok(new { success = true, totalRevenue = total, granularity, items });
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

        // GET /api/Reporting/centers/{centerId}/revenue-by-service?from=...&to=...
        [HttpGet("centers/{centerId}/revenue-by-service")]
        public async Task<IActionResult> GetRevenueByService(int centerId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            var bookings = await _bookingRepo.GetBookingsByCenterIdAsync(centerId, 1, 100000, null, start, end);

            var grouped = bookings
                .Where(b => (b.Status ?? string.Empty).ToUpperInvariant() == "COMPLETED" || (b.Status ?? string.Empty).ToUpperInvariant() == "PAID")
                .GroupBy(b => new { b.ServiceId, serviceName = b.Service?.ServiceName ?? ($"Service #{b.ServiceId}") })
                .Select(g => new {
                    serviceId = g.Key.ServiceId,
                    serviceName = g.Key.serviceName,
                    count = g.Count(),
                    revenue = g.Sum(x => (decimal)(x.Service?.BasePrice ?? 0))
                })
                .OrderByDescending(x => x.revenue)
                .ToList();

            return Ok(new { success = true, items = grouped });
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
    }
}


