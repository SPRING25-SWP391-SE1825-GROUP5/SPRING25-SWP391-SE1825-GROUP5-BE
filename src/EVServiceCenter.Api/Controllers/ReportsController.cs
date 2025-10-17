using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Api.Controllers
{
	[ApiController]
	[Route("api/reports")]
	[Authorize]
	public class ReportsController : ControllerBase
	{
		private readonly EVServiceCenter.Infrastructure.Configurations.EVDbContext _db;
		public ReportsController(EVServiceCenter.Infrastructure.Configurations.EVDbContext db)
		{
			_db = db;
		}

		// GET /api/reports/revenue?from=...&to=...&method=PAYOS|CASH
		[HttpGet("revenue")]
		public async Task<IActionResult> GetRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? method = null)
		{
			var q = _db.Payments.AsQueryable();
			q = q.Where(p => p.Status == "PAID");
			// Doanh thu ghi nhận theo thời điểm thanh toán
			if (from.HasValue) q = q.Where(p => p.PaidAt >= from.Value);
			if (to.HasValue) q = q.Where(p => p.PaidAt <= to.Value);
			if (!string.IsNullOrWhiteSpace(method)) q = q.Where(p => p.PaymentMethod == method);

			var total = await System.Threading.Tasks.Task.FromResult(q.Sum(p => (decimal)p.Amount));
			var count = await System.Threading.Tasks.Task.FromResult(q.Count());
			var byMethod = await System.Threading.Tasks.Task.FromResult(
				q.GroupBy(p => p.PaymentMethod)
				.Select(g => new { method = g.Key, total = (decimal)g.Sum(x => x.Amount), count = g.Count() })
				.ToList());

			return Ok(new { success = true, data = new { total, count, byMethod } });
		}

		// GET /api/reports/cashier?from=...&to=...&paidByUserId=...
		[HttpGet("cashier")]
		public async Task<IActionResult> GetCashierReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? paidByUserId)
		{
			var q = _db.Payments.AsQueryable();
			q = q.Where(p => p.Status == "PAID");
			if (from.HasValue) q = q.Where(p => p.CreatedAt >= from.Value);
			if (to.HasValue) q = q.Where(p => p.CreatedAt <= to.Value);
			if (paidByUserId.HasValue) q = q.Where(p => p.PaidByUserID == paidByUserId.Value);

			var total = await System.Threading.Tasks.Task.FromResult(q.Sum(p => (decimal)p.Amount));
			var count = await System.Threading.Tasks.Task.FromResult(q.Count());
			var byCashier = await System.Threading.Tasks.Task.FromResult(
				q.GroupBy(p => p.PaidByUserID)
				.Select(g => new { paidByUserId = g.Key, total = (decimal)g.Sum(x => x.Amount), count = g.Count() })
				.ToList());

			return Ok(new { success = true, data = new { total, count, byCashier } });
		}

		// ===================== ORDERS REPORTS =====================

		// GET /api/reports/orders/revenue?from=...&to=...
		// Doanh thu theo Order dựa trên Payments (Status=PAID) qua Invoice, tính theo PaidAt
		[HttpGet("orders/revenue")]
		public async Task<IActionResult> GetOrdersRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
		{
			var q = from p in _db.Payments
			        join i in _db.Invoices on p.InvoiceId equals i.InvoiceId
			        join o in _db.Orders on i.OrderId equals o.OrderId
			        where p.Status == "PAID"
			        select new { o.OrderId, p.Amount, p.PaidAt };

			if (from.HasValue) q = q.Where(x => x.PaidAt >= from.Value);
			if (to.HasValue) q = q.Where(x => x.PaidAt <= to.Value);

			var list = await q.ToListAsync();
			var totalRevenue = list.Sum(x => (decimal)x.Amount);
			var totalOrders = list.Select(x => x.OrderId).Distinct().Count();
			var aov = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

			return Ok(new { totalRevenue, totalOrders, averageOrderValue = aov });
		}

		// ===================== AGGREGATE REVENUE =====================
		// GET /api/reports/revenue/aggregate?from=...&to=...&source=payments|orders&centerIds=1,2&groupBy=date,center&method=CASH
		[HttpGet("revenue/aggregate")]
		public async Task<IActionResult> GetRevenueAggregate(
			[FromQuery] DateTime fromDate,
			[FromQuery] DateTime toDate,
			[FromQuery] string? source = "payments",
			[FromQuery] string? centerIds = null,
			[FromQuery] string? groupBy = null,
			[FromQuery] string? method = null)
		{
			if (toDate <= fromDate) return BadRequest(new { success = false, message = "to phải lớn hơn from" });

			var wantGroupDate = (groupBy ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains("date", StringComparer.OrdinalIgnoreCase);
			var wantGroupCenter = (groupBy ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains("center", StringComparer.OrdinalIgnoreCase);

			HashSet<int>? centerIdSet = null;
			if (!string.IsNullOrWhiteSpace(centerIds))
			{
				centerIdSet = new HashSet<int>(centerIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(s => int.TryParse(s, out var id) ? id : 0)
					.Where(id => id > 0));
			}

			source = (source ?? "payments").ToLowerInvariant();
			if (source != "payments" && source != "orders")
			{
				return BadRequest(new { success = false, message = "source chỉ hỗ trợ: payments|orders" });
			}

			decimal totalRevenue = 0m;
			int totalCount = 0;

			List<object> series = new();

			if (source == "payments")
			{
				// Payments (PAID) theo PaidAt; suy centerId từ Booking của Invoice (nếu có)
				var q = from p in _db.Payments
				        join i in _db.Invoices on p.InvoiceId equals i.InvoiceId
				        join b in _db.Bookings on i.BookingId equals b.BookingId into jb
				        from b in jb.DefaultIfEmpty()
					where p.Status == "PAID" && p.PaidAt >= fromDate && p.PaidAt < toDate
				        select new { p.Amount, p.PaidAt, centerId = (int?)b.CenterId, p.PaymentMethod };

				if (!string.IsNullOrWhiteSpace(method)) q = q.Where(x => x.PaymentMethod == method);
				if (centerIdSet != null && centerIdSet.Count > 0) q = q.Where(x => x.centerId.HasValue && centerIdSet.Contains(x.centerId.Value));

				var list = await q.ToListAsync();
				totalRevenue = list.Sum(x => (decimal)x.Amount);
				totalCount = list.Count;

				IEnumerable<object> enumerated = list.Select(x => new { date = x.PaidAt?.Date ?? DateTime.MinValue, centerId = x.centerId, amount = (decimal)x.Amount });

				if (wantGroupDate && wantGroupCenter)
				{
					series = enumerated
						.GroupBy(x => new { ((dynamic)x).date, ((dynamic)x).centerId })
						.Select(g => (object)new { date = g.Key.date, centerId = g.Key.centerId, revenue = g.Sum(v => (decimal)((dynamic)v).amount) })
						.OrderBy(x => ((dynamic)x).date)
						.ToList();
				}
				else if (wantGroupDate)
				{
					series = enumerated
						.GroupBy(x => ((dynamic)x).date)
						.Select(g => (object)new { date = g.Key, revenue = g.Sum(v => (decimal)((dynamic)v).amount) })
						.OrderBy(x => ((dynamic)x).date)
						.ToList();
				}
				else if (wantGroupCenter)
				{
					series = enumerated
						.GroupBy(x => ((dynamic)x).centerId)
						.Select(g => (object)new { centerId = g.Key, revenue = g.Sum(v => (decimal)((dynamic)v).amount) })
						.ToList();
				}
			}
			else // source == "orders"
			{
				// Doanh thu Order = tổng payments theo Invoice của Order (PAID)
				var q = from p in _db.Payments
				        join i in _db.Invoices on p.InvoiceId equals i.InvoiceId
				        join o in _db.Orders on i.OrderId equals o.OrderId
					where p.Status == "PAID" && p.PaidAt >= fromDate && p.PaidAt < toDate
				        select new { o.OrderId, p.Amount, p.PaidAt };

				var list = await q.ToListAsync();
				totalRevenue = list.Sum(x => (decimal)x.Amount);
				totalCount = list.Select(x => x.OrderId).Distinct().Count();

				if (wantGroupDate)
				{
					series = list
						.GroupBy(x => x.PaidAt?.Date ?? DateTime.MinValue)
						.Select(g => (object)new { date = g.Key, revenue = g.Sum(v => (decimal)v.Amount) })
						.OrderBy(x => ((dynamic)x).date)
						.ToList();
				}
			}

			return Ok(new { summary = new { totalRevenue, count = totalCount }, series });
		}

		// GET /api/reports/orders/top-parts?from=...&to=...&top=10
		[HttpGet("orders/top-parts")]
		public async Task<IActionResult> GetOrdersTopParts([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int top = 10)
		{
			if (top <= 0) top = 10;
			var allowed = new[] { "PAID", "COMPLETED", "DONE", "FINISHED" };
			var q = from oi in _db.OrderItems
			        join o in _db.Orders on oi.OrderId equals o.OrderId
			        join p in _db.Parts on oi.PartId equals p.PartId
			        where allowed.Contains(o.Status)
			        select new { oi.PartId, p.PartName, qty = oi.Quantity, revenue = (decimal)oi.Quantity * oi.UnitPrice, o.CreatedAt };

			if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
			if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);

			var result = await q
				.GroupBy(x => new { x.PartId, x.PartName })
				.Select(g => new { partId = g.Key.PartId, partName = g.Key.PartName, quantity = g.Sum(x => x.qty), revenue = g.Sum(x => x.revenue) })
				.OrderByDescending(x => x.revenue)
				.Take(top)
				.ToListAsync();

			return Ok(result);
		}

		// GET /api/reports/orders/by-customer?customerId=...&from=...&to=...&page=1&pageSize=20
		[HttpGet("orders/by-customer")]
		public async Task<IActionResult> GetOrdersByCustomer([FromQuery] int customerId, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			if (customerId <= 0) return BadRequest(new { success = false, message = "customerId bắt buộc" });
			if (page <= 0) page = 1;
			if (pageSize <= 0 || pageSize > 200) pageSize = 20;

			var baseQ = _db.Orders.Where(o => o.CustomerId == customerId && o.CreatedAt >= from && o.CreatedAt < to);
			var total = await baseQ.CountAsync();

			// Tổng hợp doanh thu từ OrderItems
			var revenue = await (from o in baseQ
							   join oi in _db.OrderItems on o.OrderId equals oi.OrderId
							   select (decimal)oi.Quantity * oi.UnitPrice).SumAsync();

			var lastOrderAt = await baseQ.OrderByDescending(o => o.CreatedAt).Select(o => (DateTime?)o.CreatedAt).FirstOrDefaultAsync();

			var items = await baseQ
				.OrderByDescending(o => o.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(o => new
				{
					orderId = o.OrderId,
					status = o.Status,
					createdAt = o.CreatedAt,
					totalAmount = _db.OrderItems.Where(oi => oi.OrderId == o.OrderId).Select(oi => (decimal)oi.Quantity * oi.UnitPrice).Sum()
				})
				.ToListAsync();

			return Ok(new
			{
				aggregates = new { orders = total, revenue, lastOrderAt },
				total = total,
				items
			});
		}
	}
}


