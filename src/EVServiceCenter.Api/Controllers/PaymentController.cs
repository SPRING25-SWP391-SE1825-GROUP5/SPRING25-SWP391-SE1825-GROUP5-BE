using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
	private readonly PaymentService _paymentService;
    private readonly IPayOSService _payOSService;
    private readonly IBookingRepository _bookingRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentService paymentService,
        IPayOSService payOSService,
        IBookingRepository bookingRepo,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo,
        ILogger<PaymentController> logger)
	{
		_paymentService = paymentService;
        _payOSService = payOSService;
        _bookingRepo = bookingRepo;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _logger = logger;
	}

	[HttpPost("booking/{bookingId:int}/link")]
	public async Task<IActionResult> CreateBookingPaymentLink([FromRoute] int bookingId)
	{
		try
		{
			var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
			if (booking == null)
			{
				return NotFound(new { success = false, message = "Không tìm thấy booking" });
			}

			var totalAmount = booking.Service?.BasePrice ?? 0;

			if (booking.AppliedCredit != null)
			{
				totalAmount -= booking.AppliedCredit.RemainingCredits;
			}

			var description = $"Thanh toán vé #{bookingId}";

			var customerName = booking.Customer?.User?.FullName ?? "Khách hàng";

			var checkoutUrl = await _payOSService.CreatePaymentLinkAsync(
				bookingId, 
				totalAmount, 
				description, 
				customerName
			);

			return Ok(new { checkoutUrl });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi tạo link thanh toán: {ex.Message}" });
		}
	}

	[HttpGet("status/{orderCode}")]
	public async Task<IActionResult> GetPaymentStatus([FromRoute] int orderCode)
	{
		try
		{
			var paymentInfo = await _payOSService.GetPaymentInfoAsync(orderCode);
			return Ok(new { success = true, data = paymentInfo });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi lấy thông tin thanh toán: {ex.Message}" });
		}
	}

	[HttpGet("qr/{orderCode}")]
	public async Task<IActionResult> GetPaymentQRCode([FromRoute] int orderCode)
	{
		try
		{
			var paymentInfo = await _payOSService.GetPaymentInfoAsync(orderCode);
			if (paymentInfo?.QrCode != null)
			{
				return Ok(new { success = true, qrCode = paymentInfo.QrCode, checkoutUrl = paymentInfo.CheckoutUrl });
			}
			return NotFound(new { success = false, message = "Không tìm thấy QR code" });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi lấy QR code: {ex.Message}" });
		}
	}

	[HttpDelete("cancel/{orderCode}")]
	public async Task<IActionResult> CancelPaymentLink([FromRoute] int orderCode)
	{
		try
		{
			var result = await _payOSService.CancelPaymentLinkAsync(orderCode);
			return Ok(new { success = result, message = result ? "Hủy link thành công" : "Hủy link thất bại" });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi hủy link: {ex.Message}" });
		}
	}

	[AllowAnonymous]
	[HttpGet("/payment/result")]
	public async Task<IActionResult> PaymentResult([FromQuery] string orderCode, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] string? desc = null)
	{
		if (string.IsNullOrWhiteSpace(orderCode))
		{
			return BadRequest(new { success = false, message = "Thiếu orderCode từ PayOS" });
		}

		var payOSConfirmed = await _payOSService.HandlePaymentCallbackAsync(orderCode);
		
		var confirmed = false;
		if (payOSConfirmed)
		{
			confirmed = await _paymentService.ConfirmPaymentAsync(orderCode);
		}

		var html = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>Kết quả thanh toán</title></head><body style=\"font-family: sans-serif; padding:24px\"><h2>Kết quả thanh toán</h2><p>OrderCode: {orderCode}</p><p>Trạng thái (PayOS): {status ?? "(không có)"}</p><p>Mã (PayOS): {code ?? "(không có)"}</p><p>Mô tả (PayOS): {desc ?? "(không có)"}</p><hr/><p>Xác nhận PayOS: {(payOSConfirmed ? "THÀNH CÔNG" : "KHÔNG THÀNH CÔNG")}</p><p>Cập nhật hệ thống: {(confirmed ? "THÀNH CÔNG" : "KHÔNG THÀNH CÔNG")}</p></body></html>";
		return Content(html, "text/html; charset=utf-8");
	}

    public class PaymentOfflineRequest
    {
        public int Amount { get; set; }
        public int PaidByUserId { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    [HttpPost("booking/{bookingId:int}/payments/offline")]
    [Authorize]
    public async Task<IActionResult> CreateOfflineForBooking([FromRoute] int bookingId, [FromBody] PaymentOfflineRequest req)
    {
        if (req == null || req.Amount <= 0 || req.PaidByUserId <= 0)
        {
            return BadRequest(new { success = false, message = "amount và paidByUserId là bắt buộc" });
        }

        var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy booking" });
        }

        var invoice = await _invoiceRepo.GetByBookingIdAsync(booking.BookingId);
        if (invoice == null)
        {
            invoice = new Domain.Entities.Invoice
            {
                BookingId = booking.BookingId,
                CustomerId = booking.CustomerId,
                Email = booking.Customer?.User?.Email,
                Phone = booking.Customer?.User?.PhoneNumber,
                Status = "PAID",
                CreatedAt = DateTime.UtcNow,
            };
            invoice = await _invoiceRepo.CreateMinimalAsync(invoice);
        }

        var payment = new Domain.Entities.Payment
        {
            PaymentCode = $"PAYCASH{DateTime.UtcNow:yyyyMMddHHmmss}{bookingId}",
            InvoiceId = invoice.InvoiceId,
            PaymentMethod = "CASH",
            Amount = req.Amount,
            Status = "PAID",
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PaidByUserID = req.PaidByUserId,
        };

        payment = await _paymentRepo.CreateAsync(payment);
        return Ok(new { paymentId = payment.PaymentId, paymentCode = payment.PaymentCode, status = payment.Status, amount = payment.Amount, paymentMethod = payment.PaymentMethod, paidByUserId = payment.PaidByUserID });
    }

	[HttpGet("check-status/{orderCode}")]
	[AllowAnonymous]
	public async Task<IActionResult> CheckStatus([FromRoute] string orderCode)
	{
		try
		{
			_logger.LogInformation("Frontend gọi API check-status cho orderCode: {OrderCode}", orderCode);
			
			var ok = await _paymentService.ConfirmPaymentAsync(orderCode);
			
			_logger.LogInformation("Kết quả confirm payment cho orderCode {OrderCode}: {Result}", orderCode, ok);
			
			return Ok(new { 
				success = true,
				orderCode, 
				updated = ok,
				message = ok ? "Thanh toán đã được xác nhận và cập nhật thành công" : "Thanh toán chưa được xác nhận"
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Lỗi khi check payment status cho orderCode: {OrderCode}", orderCode);
			return StatusCode(500, new { 
				success = false,
				orderCode,
				updated = false,
				message = "Có lỗi xảy ra khi xác nhận thanh toán: " + ex.Message
			});
		}
	}

	[HttpGet("return")]
	[AllowAnonymous]
	public async Task<IActionResult> Return([FromQuery] string orderCode, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = false)
	{
		var ok = await _paymentService.ConfirmPaymentAsync(orderCode);
		return Ok(new { success = ok, message = ok ? "Payment success processed" : "Payment not confirmed", orderCode, status, code, cancel });
	}

	[HttpGet("cancel")]
	[AllowAnonymous]
	public async Task<IActionResult> Cancel([FromQuery] string orderCode, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = true)
	{
		var _ = await _paymentService.ConfirmPaymentAsync(orderCode);
		return Ok(new { success = true, message = "Payment cancelled", orderCode, status, code, cancel });
	}
}