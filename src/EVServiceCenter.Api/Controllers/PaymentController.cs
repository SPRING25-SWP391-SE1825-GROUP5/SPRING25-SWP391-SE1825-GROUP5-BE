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

			return Ok(new { 
				success = true, 
				message = "Tạo link thanh toán thành công", 
				data = new { checkoutUrl } 
			});
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
	public async Task<IActionResult> PaymentResult([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] string? desc = null)
	{
		_logger.LogInformation("=== PAYMENT RESULT RETURN URL START ===");
		_logger.LogInformation("BookingId: {BookingId}", bookingId);
		_logger.LogInformation("Status: {Status}", status);
		_logger.LogInformation("Code: {Code}", code);
		_logger.LogInformation("Description: {Description}", desc);

		if (bookingId <= 0)
		{
			_logger.LogError("Thiếu bookingId từ PayOS");
			return BadRequest(new { success = false, message = "Thiếu bookingId từ PayOS" });
		}

		var payOSConfirmed = false;
		var confirmed = false;
		string payOSError = "";
		string systemError = "";

		// Kiểm tra trực tiếp từ PayOS parameters thay vì gọi API
		if (status == "PAID" && code == "00")
		{
			_logger.LogInformation("PayOS báo thanh toán thành công từ ReturnUrl parameters");
			payOSConfirmed = true;
		}
		else
		{
			_logger.LogWarning("PayOS parameters không cho thấy thanh toán thành công - Status: {Status}, Code: {Code}", status, code);
			payOSError = $"Status: {status}, Code: {code}";
		}
		
		if (payOSConfirmed)
		{
			try
			{
				_logger.LogInformation("Bắt đầu cập nhật hệ thống cho bookingId: {BookingId}", bookingId);
				confirmed = await _paymentService.ConfirmPaymentAsync(bookingId);
				_logger.LogInformation("System update result: {Result}", confirmed);
			}
			catch (Exception ex)
			{
				systemError = ex.Message;
				_logger.LogError(ex, "Lỗi khi cập nhật hệ thống cho bookingId: {BookingId}", bookingId);
			}
		}
		else
		{
			_logger.LogWarning("PayOS confirmation failed, không thể cập nhật hệ thống cho bookingId: {BookingId}", bookingId);
		}

		_logger.LogInformation("=== PAYMENT RESULT RETURN URL END ===");
		_logger.LogInformation("Final results - PayOS: {PayOS}, System: {System}", payOSConfirmed, confirmed);

		var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8""/>
    <title>Kết quả thanh toán</title>
    <style>
        body {{ font-family: sans-serif; padding: 24px; }}
        .error {{ color: red; }}
        .success {{ color: green; }}
        .info {{ background: #f0f0f0; padding: 10px; margin: 10px 0; }}
    </style>
</head>
<body>
    <h2>Kết quả thanh toán</h2>
    <div class=""info"">
        <p><strong>BookingId:</strong> {bookingId}</p>
        <p><strong>Trạng thái (PayOS):</strong> {status ?? "(không có)"}</p>
        <p><strong>Mã (PayOS):</strong> {code ?? "(không có)"}</p>
        <p><strong>Mô tả (PayOS):</strong> {desc ?? "(không có)"}</p>
    </div>
    <hr/>
    <p><strong>Xác nhận PayOS:</strong> <span class=""{(payOSConfirmed ? "success" : "error")}"">{(payOSConfirmed ? "THÀNH CÔNG" : "KHÔNG THÀNH CÔNG")}</span></p>
    <p><strong>Cập nhật hệ thống:</strong> <span class=""{(confirmed ? "success" : "error")}"">{(confirmed ? "THÀNH CÔNG" : "KHÔNG THÀNH CÔNG")}</span></p>
    {(string.IsNullOrEmpty(payOSError) ? "" : $"<p class=\"error\"><strong>Lỗi PayOS:</strong> {payOSError}</p>")}
    {(string.IsNullOrEmpty(systemError) ? "" : $"<p class=\"error\"><strong>Lỗi hệ thống:</strong> {systemError}</p>")}
</body>
</html>";
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

	[HttpGet("check-status/{bookingId}")]
	[AllowAnonymous]
	public async Task<IActionResult> CheckStatus([FromRoute] int bookingId)
	{
		try
		{
			_logger.LogInformation("Frontend gọi API check-status cho bookingId: {BookingId}", bookingId);
			
			var ok = await _paymentService.ConfirmPaymentAsync(bookingId);
			
			_logger.LogInformation("Kết quả confirm payment cho bookingId {BookingId}: {Result}", bookingId, ok);
			
			return Ok(new { 
				success = true,
				bookingId, 
				updated = ok,
				message = ok ? "Thanh toán đã được xác nhận và cập nhật thành công" : "Thanh toán chưa được xác nhận"
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Lỗi khi check payment status cho bookingId: {BookingId}", bookingId);
			return StatusCode(500, new { 
				success = false,
				bookingId,
				updated = false,
				message = "Có lỗi xảy ra khi xác nhận thanh toán: " + ex.Message
			});
		}
	}

	[HttpGet("return")]
	[AllowAnonymous]
	public async Task<IActionResult> Return([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = false)
	{
		var ok = await _paymentService.ConfirmPaymentAsync(bookingId);
		return Ok(new { success = ok, message = ok ? "Payment success processed" : "Payment not confirmed", bookingId, status, code, cancel });
	}

	[HttpGet("cancel")]
	[AllowAnonymous]
	public async Task<IActionResult> Cancel([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = true)
	{
		var _ = await _paymentService.ConfirmPaymentAsync(bookingId);
		return Ok(new { success = true, message = "Payment cancelled", bookingId, status, code, cancel });
	}
}