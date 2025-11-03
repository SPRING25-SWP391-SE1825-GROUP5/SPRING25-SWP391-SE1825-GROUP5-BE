using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Application.Models.Requests;

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
    private readonly IWorkOrderPartRepository _workOrderPartRepo;
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentService paymentService,
        IPayOSService payOSService,
        IBookingRepository bookingRepo,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo,
        IWorkOrderPartRepository workOrderPartRepo,
        ICustomerServiceCreditRepository customerServiceCreditRepo,
        IConfiguration configuration,
        ILogger<PaymentController> logger)
	{
		_paymentService = paymentService;
        _payOSService = payOSService;
        _bookingRepo = bookingRepo;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _workOrderPartRepo = workOrderPartRepo;
        _customerServiceCreditRepo = customerServiceCreditRepo;
        _configuration = configuration;
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

	/// <summary>
	/// Tạo QR code thanh toán SePay cho Booking
	/// Tương tự CreateBookingPaymentLink nhưng dùng SePay QR code thay vì PayOS
	/// </summary>
	[HttpPost("booking/{bookingId:int}/sepay-qr")]
	[Authorize]
	public async Task<IActionResult> CreateSePayQrCode([FromRoute] int bookingId)
	{
		try
		{
			var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
			if (booking == null)
			{
				return NotFound(new { success = false, message = "Không tìm thấy booking" });
			}

			if (booking.Status == "CANCELLED")
			{
				return BadRequest(new { success = false, message = "Booking đã bị hủy" });
			}

			if (booking.Status == "PAID")
			{
				return BadRequest(new { success = false, message = "Booking đã được thanh toán" });
			}

			// Tính tổng tiền theo logic giống PayOS
			var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
			decimal packageDiscountAmount = 0m;
			decimal partsAmount = 0m;

			// Tính parts amount
			var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
			if (workOrderParts != null && workOrderParts.Any())
			{
				partsAmount = workOrderParts.Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));
			}

			// Tính package discount nếu có
			if (booking.AppliedCreditId.HasValue)
			{
				var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
				if (appliedCredit?.ServicePackage != null)
				{
					packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
				}
			}

			// Total = (dùng gói: packageDiscountAmount; dùng lẻ: serviceBasePrice) + parts
			decimal totalAmount = (booking.AppliedCreditId.HasValue ? packageDiscountAmount : serviceBasePrice) + partsAmount;

			var amount = (int)Math.Round(totalAmount);
			if (amount < 1000) amount = 1000; // Min amount

			// Tạo transaction content (nội dung chuyển khoản)
			// Format: Pay{bookingId}ment để SePay có thể parse bookingId từ webhook
			var transactionContent = $"Pay{bookingId}ment";

			// Lấy cấu hình SePay từ appsettings
			var sepayAccount = _configuration["SePay:Account"] ?? "0888294028";
			var sepayBank = _configuration["SePay:Bank"] ?? "VPBank";
			var sepayBeneficiary = _configuration["SePay:Beneficiary"] ?? "SEPAY COMPANY";
			var qrCodeBaseUrl = _configuration["SePay:QrCodeBaseUrl"] ?? "https://qr.sepay.vn/img";

			// Tạo QR code URL từ SePay
			// Format: https://qr.sepay.vn/img?acc={account}&bank={bank}&amount={amount}&des={description}
			var qrCodeUrl = $"{qrCodeBaseUrl}?acc={Uri.EscapeDataString(sepayAccount)}&bank={Uri.EscapeDataString(sepayBank)}&amount={amount}&des={Uri.EscapeDataString(transactionContent)}";

			_logger.LogInformation("SePay QR Code created for booking {BookingId}: Amount={Amount}, Content={Content}", bookingId, amount, transactionContent);

			return Ok(new
			{
				success = true,
				message = "Tạo QR code thanh toán thành công",
				data = new
				{
					qrCodeUrl = qrCodeUrl,
					bookingId = bookingId,
					amount = amount,
					transactionContent = transactionContent,
					bank = $"{sepayBank} - {sepayAccount} - {sepayBeneficiary}",
					instructions = "Quét mã QR để thực hiện chuyển khoản. Vui lòng nhập đúng nội dung chuyển khoản để hệ thống xác nhận thanh toán tự động."
				}
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating SePay QR code for booking {BookingId}", bookingId);
			return StatusCode(500, new { success = false, message = $"Lỗi tạo QR code thanh toán: {ex.Message}" });
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

	/// <summary>
	/// Webhook endpoint để nhận callback từ SePay khi có giao dịch thanh toán
	/// URL: https://spring25-swp391-se1825-group5-be.onrender.com/api/payment/sepay-webhook
	/// </summary>
	[HttpPost("/api/payment/sepay-webhook")]
	[AllowAnonymous]
	public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookRequest request)
	{
		try
		{
			// Log webhook payload để debug
			_logger.LogInformation("=== SEPAY WEBHOOK RECEIVED ===");
			_logger.LogInformation("SePay Webhook Data: {WebhookData}", System.Text.Json.JsonSerializer.Serialize(request));

			// Validate request
			if (request == null)
			{
				_logger.LogWarning("SePay webhook received null request");
				return BadRequest(new { success = false, message = "Invalid webhook payload" });
			}

			// Extract bookingId từ webhook payload
			// SePay có thể gửi bookingId trong các field khác nhau tùy vào cách config
			// Format từ CreateSePayQrCode: "Pay{bookingId}ment" trong Description
			// Thử parse từ các field phổ biến: bookingId, orderCode, transactionId, referenceId, description
			int? bookingId = null;

			if (!string.IsNullOrEmpty(request.BookingId))
			{
				if (int.TryParse(request.BookingId, out var parsedBookingId))
				{
					bookingId = parsedBookingId;
				}
			}
			else if (request.OrderCode > 0)
			{
				bookingId = request.OrderCode;
			}
			else if (!string.IsNullOrEmpty(request.Description))
			{
				// Parse từ Description với format "Pay{bookingId}ment"
				// Ví dụ: "Pay123ment" -> bookingId = 123
				if (request.Description.StartsWith("Pay", StringComparison.OrdinalIgnoreCase)
					&& request.Description.EndsWith("ment", StringComparison.OrdinalIgnoreCase))
				{
					var content = request.Description.Substring(3, request.Description.Length - 7); // Bỏ "Pay" và "ment"
					if (int.TryParse(content, out var extractedBookingId))
					{
						bookingId = extractedBookingId;
					}
				}
			}
			else if (!string.IsNullOrEmpty(request.TransactionId))
			{
				// Nếu transactionId có format chứa bookingId (ví dụ: "BOOKING_123")
				if (request.TransactionId.Contains("_"))
				{
					var parts = request.TransactionId.Split('_');
					if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out var extractedBookingId))
					{
						bookingId = extractedBookingId;
					}
				}
				else if (int.TryParse(request.TransactionId, out var transactionAsBookingId))
				{
					bookingId = transactionAsBookingId;
				}
			}
			else if (!string.IsNullOrEmpty(request.ReferenceId))
			{
				if (int.TryParse(request.ReferenceId, out var referenceAsBookingId))
				{
					bookingId = referenceAsBookingId;
				}
			}

			if (!bookingId.HasValue || bookingId.Value <= 0)
			{
				_logger.LogWarning("SePay webhook: Cannot extract bookingId from payload. Data: {Data}", System.Text.Json.JsonSerializer.Serialize(request));
				return BadRequest(new { success = false, message = "Cannot extract bookingId from webhook payload" });
			}

			_logger.LogInformation("SePay webhook: Extracted bookingId = {BookingId}", bookingId.Value);

			// Kiểm tra trạng thái thanh toán từ SePay
			// SePay thường gửi status: "SUCCESS", "PAID", "COMPLETED" cho thanh toán thành công
			var paymentStatus = request.Status?.ToUpperInvariant() ?? "";
			var isPaymentSuccess = paymentStatus == "SUCCESS"
				|| paymentStatus == "PAID"
				|| paymentStatus == "COMPLETED"
				|| paymentStatus == "00"  // Nếu SePay dùng code 00 như PayOS
				|| (request.Code == "00");

			if (!isPaymentSuccess)
			{
				_logger.LogInformation("SePay webhook: Payment not successful. Status: {Status}, Code: {Code}", paymentStatus, request.Code);
				// Trả về 200 OK để SePay không retry, nhưng không xử lý payment
				return Ok(new { success = true, message = "Webhook received but payment not successful" });
			}

			// Xác nhận thanh toán
			_logger.LogInformation("SePay webhook: Payment successful, confirming payment for booking {BookingId}", bookingId.Value);
			var confirmed = await _paymentService.ConfirmPaymentAsync(bookingId.Value);

			if (confirmed)
			{
				_logger.LogInformation("SePay webhook: Payment confirmed successfully for booking {BookingId}", bookingId.Value);
				// Trả về HTTP 200-299 để SePay biết đã nhận được thành công
				return Ok(new { success = true, message = "Payment confirmed successfully", bookingId = bookingId.Value });
			}
			else
			{
				_logger.LogWarning("SePay webhook: Failed to confirm payment for booking {BookingId}", bookingId.Value);
				// Trả về 200 OK nhưng với success = false để SePay không retry
				// Nếu muốn SePay retry, có thể trả về 500
				return Ok(new { success = false, message = "Failed to confirm payment", bookingId = bookingId.Value });
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing SePay webhook");
			// Trả về 500 để SePay retry webhook
			return StatusCode(500, new { success = false, message = "Internal server error processing webhook" });
		}
	}

	/// <summary>
	/// DTO cho SePay webhook request
	/// Cấu trúc này có thể cần điều chỉnh dựa trên format thực tế của SePay
	/// Không cần Signature vì không dùng API Key authentication
	/// </summary>
	public class SePayWebhookRequest
	{
		public string? TransactionId { get; set; }
		public string? BookingId { get; set; }
		public int OrderCode { get; set; }
		public string? ReferenceId { get; set; }
		public string? Status { get; set; }
		public string? Code { get; set; }
		public decimal? Amount { get; set; }
		public string? Description { get; set; }
		public DateTime? PaymentDate { get; set; }
	}

}
