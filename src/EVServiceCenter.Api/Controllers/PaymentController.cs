using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    // WorkOrderRepository removed - functionality merged into BookingRepository
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IPaymentRepository _paymentRepo;

    public PaymentController(PaymentService paymentService,
        IPayOSService payOSService,
        IBookingRepository bookingRepo,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo)
	{
		_paymentService = paymentService;
        _payOSService = payOSService;
        _bookingRepo = bookingRepo;
        // WorkOrderRepository removed - functionality merged into BookingRepository
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
	}

	// Tạo link thanh toán PayOS cho Booking (sử dụng PayOSService mới)
	[HttpPost("booking/{bookingId:int}/link")]
	public async Task<IActionResult> CreateBookingPaymentLink([FromRoute] int bookingId)
	{
		try
		{
			// Lấy thông tin booking
			var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
			if (booking == null)
			{
				return NotFound(new { success = false, message = "Không tìm thấy booking" });
			}

			// Tính tổng tiền từ service
			var totalAmount = booking.Service?.BasePrice ?? 0;

			// Trừ credit nếu có (sử dụng remaining credits)
			if (booking.AppliedCredit != null)
			{
				totalAmount -= booking.AppliedCredit.RemainingCredits;
			}

			// Tạo description (giống code mẫu - giới hạn 25 ký tự)
			var description = $"Thanh toán vé #{bookingId}";

			// Lấy tên khách hàng
			var customerName = booking.Customer?.User?.FullName ?? "Khách hàng";

			// Tạo PayOS payment link (giống code mẫu)
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

	// Lấy thông tin thanh toán từ PayOS (giống code mẫu)
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

	// Lấy QR code từ PayOS (giống code mẫu)
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

	// Hủy link thanh toán PayOS (giống code mẫu)
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

	// ReturnUrl: PayOS redirect về đây sau khi thanh toán (KHÔNG dùng webhook)
	// Cho phép anonymous vì PayOS gọi từ trình duyệt người dùng
	[AllowAnonymous]
	[HttpGet("/payment/result")]
	public async Task<IActionResult> PaymentResult([FromQuery] string orderCode, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] string? desc = null)
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

    public class PaymentOfflineRequest
    {
        public int Amount { get; set; }
        public int PaidByUserId { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    // Ghi nhận thanh toán offline cho booking: tự đảm bảo invoice tồn tại
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

        // WorkOrder functionality merged into Booking - no separate work order needed
        // Booking already contains all necessary information

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

	// (Tuỳ chọn) Kiểm tra trạng thái theo orderCode nếu FE cần hỏi lại
	[HttpGet("check-status/{orderCode}")]
	public async Task<IActionResult> CheckStatus([FromRoute] string orderCode)
	{
		var ok = await _paymentService.ConfirmPaymentAsync(orderCode);
		return Ok(new { orderCode, updated = ok });
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
		// For cancel route, still call confirm to fetch status and let service no-op if not paid
		var _ = await _paymentService.ConfirmPaymentAsync(orderCode);
		return Ok(new { success = true, message = "Payment cancelled", orderCode, status, code, cancel });
	}
}


