using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
	private readonly PaymentService _paymentService;
    private readonly IBookingRepository _bookingRepo;
    private readonly IWorkOrderRepository _workOrderRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IPaymentRepository _paymentRepo;

    public PaymentController(PaymentService paymentService,
        IBookingRepository bookingRepo,
        IWorkOrderRepository workOrderRepo,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo)
	{
		_paymentService = paymentService;
        _bookingRepo = bookingRepo;
        _workOrderRepo = workOrderRepo;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
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

        // Ensure WorkOrder exists (re-use logic from PaymentService style)
        var workOrder = await _workOrderRepo.GetByBookingIdAsync(bookingId);
        if (workOrder == null)
        {
            workOrder = new Domain.Entities.WorkOrder
            {
                BookingId = booking.BookingId,

                Status = "OPEN",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            workOrder = await _workOrderRepo.CreateAsync(workOrder);
        }

        var invoice = await _invoiceRepo.GetByBookingIdAsync(booking.BookingId);
        if (invoice == null)
        {
            invoice = new Domain.Entities.Invoice
            {
                WorkOrderId = workOrder.WorkOrderId,
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
	[HttpGet("status/{orderCode}")]
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


