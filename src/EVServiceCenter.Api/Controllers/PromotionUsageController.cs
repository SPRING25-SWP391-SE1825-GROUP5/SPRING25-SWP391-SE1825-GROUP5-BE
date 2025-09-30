using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
	[ApiController]
	[Route("api/promotions/usage")]
	[Authorize]
	public class PromotionUsageController : ControllerBase
	{
		private readonly IPromotionRepository _promotionRepo;

		public PromotionUsageController(IPromotionRepository promotionRepo)
		{
			_promotionRepo = promotionRepo;
		}

		[HttpGet]
		public async Task<IActionResult> GetByCustomer([FromQuery] int customerId)
		{
			if (customerId <= 0) return BadRequest(new { success = false, message = "customerId không hợp lệ" });
			var items = await _promotionRepo.GetUserPromotionsByCustomerAsync(customerId);
			var result = items.Select(x => new
			{
				code = x.Promotion?.Code,
				description = x.Promotion?.Description,
				invoiceId = x.InvoiceId,
				discountAmount = x.DiscountAmount,
				usedAt = x.UsedAt,
				status = x.Status
			});
			return Ok(new { success = true, data = result });
		}
	}
}


