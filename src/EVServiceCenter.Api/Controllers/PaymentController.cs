using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Service;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
	private readonly PaymentService _paymentService;

	public PaymentController(PaymentService paymentService)
	{
		_paymentService = paymentService;
	}

	// Tạo link thanh toán PayOS cho Booking
	[HttpPost("booking/{bookingId:int}/link")]
	public async Task<IActionResult> CreateBookingPaymentLink([FromRoute] int bookingId)
	{
		var checkoutUrl = await _paymentService.CreateBookingPaymentLinkAsync(bookingId);
		return Ok(new { checkoutUrl });
	}

	// ReturnUrl: PayOS redirect về đây sau khi thanh toán (KHÔNG dùng webhook)
	// Cho phép anonymous vì PayOS gọi từ trình duyệt người dùng
	[AllowAnonymous]
	[HttpGet("/payment/result")]
	public async Task<IActionResult> PaymentResult([FromQuery] string orderCode, [FromQuery] string status = null, [FromQuery] string code = null, [FromQuery] string desc = null)
	{
		if (string.IsNullOrWhiteSpace(orderCode))
		{
			return BadRequest(new { success = false, message = "Thiếu orderCode từ PayOS" });
		}

		var confirmed = await _paymentService.ConfirmPaymentAsync(orderCode);

		// Trả về HTML đơn giản để người dùng thấy kết quả ngay cả khi không có FE
		var html = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>Kết quả thanh toán</title></head><body style=\"font-family: sans-serif; padding:24px\"><h2>Kết quả thanh toán</h2><p>OrderCode: {orderCode}</p><p>Trạng thái (PayOS): {status ?? "(không có)"}</p><p>Mã (PayOS): {code ?? "(không có)"}</p><p>Mô tả (PayOS): {desc ?? "(không có)"}</p><hr/><p>Cập nhật hệ thống: {(confirmed ? "THÀNH CÔNG" : "KHÔNG THÀNH CÔNG")}</p></body></html>";
		return Content(html, "text/html; charset=utf-8");
	}

	// (Tuỳ chọn) Kiểm tra trạng thái theo orderCode nếu FE cần hỏi lại
	[HttpGet("status/{orderCode}")]
	public async Task<IActionResult> CheckStatus([FromRoute] string orderCode)
	{
		var ok = await _paymentService.ConfirmPaymentAsync(orderCode);
		return Ok(new { orderCode, updated = ok });
	}
}


