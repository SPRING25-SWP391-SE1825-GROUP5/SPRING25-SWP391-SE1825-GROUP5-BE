using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public PaymentController(PaymentService paymentService,
        IPayOSService payOSService,
        IBookingRepository bookingRepo,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo,
        IConfiguration configuration)
	{
		_paymentService = paymentService;
        _payOSService = payOSService;
        _bookingRepo = bookingRepo;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _configuration = configuration;
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




	[AllowAnonymous]
	[HttpGet("/api/payment/result")]
	public async Task<IActionResult> PaymentResult([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] string? desc = null)
	{
		if (bookingId <= 0)
		{
			return BadRequest(new { success = false, message = "Thiếu bookingId từ PayOS" });
		}

		var payOSConfirmed = status == "PAID" && code == "00";
		var confirmed = false;
		
		if (payOSConfirmed)
		{
			try
			{
				confirmed = await _paymentService.ConfirmPaymentAsync(bookingId);
			}
			catch (Exception)
			{
				// Handle error silently
			}
		}

		var frontendUrl = _configuration["App:FrontendUrl"];
		
		if (payOSConfirmed && confirmed)
		{
			var successPath = _configuration["App:PaymentRedirects:SuccessPath"];
			var frontendSuccessUrl = $"{frontendUrl}{successPath}?bookingId={bookingId}&status=success";
			return Redirect(frontendSuccessUrl);
		}
		else if (payOSConfirmed && !confirmed)
		{
			var errorPath = _configuration["App:PaymentRedirects:ErrorPath"];
			var frontendErrorUrl = $"{frontendUrl}{errorPath}?bookingId={bookingId}&error=system_error";
			return Redirect(frontendErrorUrl);
		}
		else
		{
			var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
			var frontendFailUrl = $"{frontendUrl}{failedPath}?bookingId={bookingId}&status={status}&code={code}";
			return Redirect(frontendFailUrl);
		}
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



	[HttpGet("/api/payment/cancel")]
	[AllowAnonymous]
	public IActionResult Cancel([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = true)
	{
		// Redirect về trang hủy thanh toán trên FE
		var frontendUrl = _configuration["App:FrontendUrl"];
		var cancelledPath = _configuration["App:PaymentRedirects:CancelledPath"];
		var frontendCancelUrl = $"{frontendUrl}{cancelledPath}?bookingId={bookingId}&status={status}&code={code}";
		return Redirect(frontendCancelUrl);
	}
}