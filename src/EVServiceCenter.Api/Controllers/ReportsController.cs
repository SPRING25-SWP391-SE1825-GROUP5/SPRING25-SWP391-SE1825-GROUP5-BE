using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
	[ApiController]
	[Route("api/reports")]
	[Authorize]
	public class ReportsController : ControllerBase
	{
		private readonly EVDbContext _db;
		public ReportsController(EVDbContext db)
		{
			_db = db;
		}

		// GET /api/reports/revenue?from=...&to=...&method=PAYOS|CASH
		[HttpGet("revenue")]
		public async Task<IActionResult> GetRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string method = null)
		{
			var q = _db.Payments.AsQueryable();
			q = q.Where(p => p.Status == "PAID");
			if (from.HasValue) q = q.Where(p => p.CreatedAt >= from.Value);
			if (to.HasValue) q = q.Where(p => p.CreatedAt <= to.Value);
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
			if (paidByUserId.HasValue) q = q.Where(p => p.PaidByUserId == paidByUserId.Value);

			var total = await System.Threading.Tasks.Task.FromResult(q.Sum(p => (decimal)p.Amount));
			var count = await System.Threading.Tasks.Task.FromResult(q.Count());
			var byCashier = await System.Threading.Tasks.Task.FromResult(
				q.GroupBy(p => p.PaidByUserId)
				.Select(g => new { paidByUserId = g.Key, total = (decimal)g.Sum(x => x.Amount), count = g.Count() })
				.ToList());

			return Ok(new { success = true, data = new { total, count, byCashier } });
		}
	}
}


