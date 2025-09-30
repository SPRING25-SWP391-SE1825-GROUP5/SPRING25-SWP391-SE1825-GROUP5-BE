using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
	[ApiController]
	[Route("api/invoices/{invoiceId:int}/promotions")]
	[Authorize]
	public class InvoicePromotionsController : ControllerBase
	{
		private readonly IInvoiceRepository _invoiceRepo;
		private readonly IPromotionRepository _promotionRepo;
		private readonly EVServiceCenter.Application.Service.PromotionService _promotionService;

		public InvoicePromotionsController(
			IInvoiceRepository invoiceRepo,
			IPromotionRepository promotionRepo,
			EVServiceCenter.Application.Service.PromotionService promotionService)
		{
			_invoiceRepo = invoiceRepo;
			_promotionRepo = promotionRepo;
			_promotionService = promotionService;
		}

		public class ApplyPromotionRequest
		{
			public string Code { get; set; }
		}

		[HttpPost("apply")]
		public async Task<IActionResult> Apply(int invoiceId, [FromBody] ApplyPromotionRequest request)
		{
			if (string.IsNullOrWhiteSpace(request?.Code))
				return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

			var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
			if (invoice == null) return NotFound(new { success = false, message = "Invoice không tồn tại" });

			var validate = await _promotionService.ValidatePromotionAsync(new EVServiceCenter.Application.Models.Requests.ValidatePromotionRequest
			{
				Code = request.Code.Trim().ToUpper(),
				OrderAmount = invoice.TotalAmount,
				OrderType = "INVOICE"
			});
			if (!validate.IsValid)
				return BadRequest(new { success = false, message = validate.Message, data = validate });

			var promoEntity = await _promotionRepo.GetPromotionByCodeAsync(request.Code.Trim().ToUpper());
			if (promoEntity == null) return NotFound(new { success = false, message = "Mã khuyến mãi không tồn tại" });

			var userPromotion = new EVServiceCenter.Domain.Entities.UserPromotion
			{
				CustomerId = invoice.CustomerId ?? 0,
				PromotionId = promoEntity.PromotionId,
				InvoiceId = invoice.InvoiceId,
				UsedAt = DateTime.UtcNow,
				DiscountAmount = validate.DiscountAmount,
				Status = "APPLIED"
			};
			await _promotionRepo.CreateUserPromotionAsync(userPromotion);

			// Optionally update invoice totals here if you keep a net total column
			return Ok(new { success = true, message = "Áp dụng khuyến mãi thành công", data = validate });
		}

		[HttpDelete("{promotionCode}")]
		public async Task<IActionResult> Remove(int invoiceId, string promotionCode)
		{
			if (string.IsNullOrWhiteSpace(promotionCode))
				return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

			var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
			if (invoice == null) return NotFound(new { success = false, message = "Invoice không tồn tại" });

			var removed = await _promotionRepo.DeleteUserPromotionByInvoiceAndCodeAsync(invoiceId, promotionCode.Trim().ToUpper());
			if (!removed) return NotFound(new { success = false, message = "Không tìm thấy khuyến mãi trên hóa đơn" });

			return Ok(new { success = true, message = "Đã gỡ khuyến mãi khỏi hóa đơn" });
		}

		[HttpGet]
		public async Task<IActionResult> ListByInvoice(int invoiceId)
		{
			var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
			if (invoice == null) return NotFound(new { success = false, message = "Invoice không tồn tại" });

			var items = await _promotionRepo.GetUserPromotionsByInvoiceAsync(invoiceId);
			var result = items.Select(x => new
			{
				code = x.Promotion?.Code,
				description = x.Promotion?.Description,
				discountAmount = x.DiscountAmount,
				usedAt = x.UsedAt,
				status = x.Status
			});
			return Ok(new { success = true, data = result });
		}
	}
}


