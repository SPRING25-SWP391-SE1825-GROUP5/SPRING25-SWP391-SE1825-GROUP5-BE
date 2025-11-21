using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Constants;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
	private readonly PaymentService _paymentService;
    private readonly IPayOSService _payOSService;
    private readonly IBookingRepository _bookingRepo;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IWorkOrderPartRepository _workOrderPartRepo;
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepo;
    private readonly IPromotionRepository _promotionRepo;
    private readonly IPromotionService _promotionService;
    private readonly IEmailService _emailService;
    private readonly IPdfInvoiceService _pdfInvoiceService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    public PaymentController(PaymentService paymentService,
        IPayOSService payOSService,
        IBookingRepository bookingRepo,
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo,
        IWorkOrderPartRepository workOrderPartRepo,
        ICustomerServiceCreditRepository customerServiceCreditRepo,
        IPromotionRepository promotionRepo,
        IPromotionService promotionService,
        IEmailService emailService,
        IPdfInvoiceService pdfInvoiceService,
        INotificationService notificationService,
        IConfiguration configuration)
	{
		_paymentService = paymentService;
        _payOSService = payOSService;
        _bookingRepo = bookingRepo;
        _orderRepository = orderRepository;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _workOrderPartRepo = workOrderPartRepo;
        _customerServiceCreditRepo = customerServiceCreditRepo;
        _promotionRepo = promotionRepo;
        _promotionService = promotionService;
        _emailService = emailService;
        _pdfInvoiceService = pdfInvoiceService;
        _notificationService = notificationService;
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

			if (booking.Status != BookingStatusConstants.Completed)
			{
				return BadRequest(new {
					success = false,
					message = $"Chỉ có thể tạo payment link khi booking đã hoàn thành ({BookingStatusConstants.Completed}). Trạng thái hiện tại: {booking.Status ?? "N/A"}"
				});
			}

            var checkoutUrl = await _paymentService.CreateBookingPaymentLinkAsync(bookingId);

			return Ok(new {
				success = true,
				message = checkoutUrl != null ? "Link thanh toán đã sẵn sàng" : "Tạo link thanh toán thành công",
				data = new { checkoutUrl }
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi tạo link thanh toán: {ex.Message}" });
		}
	}

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

			if (booking.Status != BookingStatusConstants.Completed)
			{
				return BadRequest(new {
					success = false,
					message = $"Chỉ có thể tạo QR code thanh toán khi booking đã hoàn thành ({BookingStatusConstants.Completed}). Trạng thái hiện tại: {booking.Status ?? "N/A"}"
				});
			}

			var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
			decimal packageDiscountAmount = 0m;
			decimal packagePrice = 0m;
			decimal partsAmount = 0m;
			decimal promotionDiscountAmount = 0m;

            var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
            if (workOrderParts != null && workOrderParts.Any())
            {
                partsAmount = workOrderParts
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
                    .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));
            }

			if (booking.AppliedCreditId.HasValue)
			{
				var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
				if (appliedCredit?.ServicePackage != null)
				{
					packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);

					if (appliedCredit.UsedCredits == 0)
					{
						packagePrice = appliedCredit.ServicePackage.Price;
					}
				}
			}

			var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
			if (userPromotions != null && userPromotions.Any())
			{
				promotionDiscountAmount = userPromotions
					.Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
					.Sum(up => up.DiscountAmount);
			}

            var finalServicePrice = booking.AppliedCreditId.HasValue
                ? (serviceBasePrice - packageDiscountAmount)
                : serviceBasePrice;

            if (promotionDiscountAmount > finalServicePrice)
            {
                promotionDiscountAmount = finalServicePrice;
            }
            decimal totalAmount = packagePrice + finalServicePrice + partsAmount - promotionDiscountAmount;

			var amount = (int)Math.Round(totalAmount);
			if (amount < EVServiceCenter.Application.Constants.AppConstants.PaymentAmounts.MinAmountVnd)
				amount = EVServiceCenter.Application.Constants.AppConstants.PaymentAmounts.MinAmountVnd;

			var transactionContent = string.Format(EVServiceCenter.Application.Constants.AppConstants.TransactionContent.Format, bookingId);

			var sepayAccount = _configuration["SePay:Account"] ?? SePayConstants.DefaultAccount;
			var sepayBank = _configuration["SePay:Bank"] ?? SePayConstants.DefaultBank;
			var sepayBeneficiary = _configuration["SePay:Beneficiary"] ?? SePayConstants.DefaultBeneficiary;
			var qrCodeBaseUrl = _configuration["SePay:QrCodeBaseUrl"] ?? SePayConstants.DefaultQrCodeBaseUrl;

			var qrCodeUrl = $"{qrCodeBaseUrl}?acc={Uri.EscapeDataString(sepayAccount)}&bank={Uri.EscapeDataString(sepayBank)}&amount={amount}&des={Uri.EscapeDataString(transactionContent)}";

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
			return StatusCode(500, new { success = false, message = $"Lỗi tạo QR code thanh toán: {ex.Message}" });
		}
	}



    [HttpPost("booking/{bookingId:int}/cancel-link")]
    public async Task<IActionResult> CancelBookingPaymentLink([FromRoute] int bookingId)
    {
        try
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking?.PayOSOrderCode == null)
            {
                return BadRequest(new { success = false, message = "Booking không có PayOSOrderCode" });
            }
            var ok = await _payOSService.CancelPaymentLinkAsync(booking.PayOSOrderCode.Value);
            if (ok)
            {
                return Ok(new { success = true, message = "Đã hủy link PayOS hiện tại" });
            }
            return StatusCode(502, new { success = false, message = "Hủy link PayOS thất bại hoặc link không tồn tại" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi hủy link thanh toán: {ex.Message}" });
        }
    }



	[AllowAnonymous]
	[HttpGet("/api/payment/result")]
	public async Task<IActionResult> PaymentResult([FromQuery] int? bookingId, [FromQuery] int? orderCode, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] string? desc = null)
	{
		var payOSConfirmed = status == PaymentConstants.PaymentStatus.Paid && code == AppConstants.PaymentResponseCodes.Success;
		var confirmed = false;
		var frontendUrl = _configuration["App:FrontendUrl"];

        if (orderCode.HasValue && orderCode.Value > 0)
		{
			var bookingByPayOSCode = await _bookingRepo.GetBookingByPayOSOrderCodeAsync(orderCode.Value);
			var orderByPayOSCode = await _orderRepository.GetOrderByPayOSOrderCodeAsync(orderCode.Value);

			var order = orderByPayOSCode;
			var booking = bookingId.HasValue && bookingId.Value > 0
				? await _bookingRepo.GetBookingByIdAsync(bookingId.Value)
				: bookingByPayOSCode;

			if (order != null)
			{
				var actualOrderId = order.OrderId;
				if (payOSConfirmed)
				{
					try
					{
						confirmed = await _paymentService.ConfirmOrderPaymentAsync(actualOrderId);
					}
					catch (Exception)
					{
					}
				}

				if (payOSConfirmed && confirmed)
				{
					var successPath = _configuration["App:PaymentRedirects:SuccessPath"];
					var frontendSuccessUrl = $"{frontendUrl}{successPath}?orderId={actualOrderId}&status=success";
					return Redirect(frontendSuccessUrl);
				}
				else if (payOSConfirmed && !confirmed)
				{
					var errorPath = _configuration["App:PaymentRedirects:ErrorPath"];
					var frontendErrorUrl = $"{frontendUrl}{errorPath}?orderId={actualOrderId}&error=system_error";
					return Redirect(frontendErrorUrl);
				}
				else
				{
					var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
					var frontendFailUrl = $"{frontendUrl}{failedPath}?orderId={actualOrderId}&status={status}&code={code}";
					return Redirect(frontendFailUrl);
				}
			}
            else
			{

                if (booking != null)
				{
                    if (payOSConfirmed)
                    {
                        try
                        {
                            confirmed = await _paymentService.ConfirmPaymentAsync(booking.BookingId);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (payOSConfirmed && confirmed)
                    {
                        var successPath = _configuration["App:PaymentRedirects:SuccessPath"];
                        var frontendSuccessUrl = $"{frontendUrl}{successPath}?bookingId={booking.BookingId}&status=success";
                        return Redirect(frontendSuccessUrl);
                    }
                    else if (payOSConfirmed && !confirmed)
                    {
                        var errorPath = _configuration["App:PaymentRedirects:ErrorPath"];
                        var frontendErrorUrl = $"{frontendUrl}{errorPath}?bookingId={booking.BookingId}&error=system_error";
                        return Redirect(frontendErrorUrl);
                    }
                    else
                    {
                        var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
                        var frontendFailUrl = $"{frontendUrl}{failedPath}?bookingId={booking.BookingId}&status={status}&code={code}";
                        return Redirect(frontendFailUrl);
                    }
				}
				else
				{
                    return BadRequest(new { success = false, message = "Không tìm thấy order hoặc booking với orderCode: " + orderCode.Value });
				}
			}
		}
		else if (bookingId.HasValue && bookingId.Value > 0)
		{
			if (payOSConfirmed)
			{
				try
				{
					confirmed = await _paymentService.ConfirmPaymentAsync(bookingId.Value);
				}
				catch (Exception)
				{
				}
			}

			if (payOSConfirmed && confirmed)
			{
				var successPath = _configuration["App:PaymentRedirects:SuccessPath"];
				var frontendSuccessUrl = $"{frontendUrl}{successPath}?bookingId={bookingId.Value}&status=success";
				return Redirect(frontendSuccessUrl);
			}
			else if (payOSConfirmed && !confirmed)
			{
				var errorPath = _configuration["App:PaymentRedirects:ErrorPath"];
				var frontendErrorUrl = $"{frontendUrl}{errorPath}?bookingId={bookingId.Value}&error=system_error";
				return Redirect(frontendErrorUrl);
			}
			else
			{
				var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
				var frontendFailUrl = $"{frontendUrl}{failedPath}?bookingId={bookingId.Value}&status={status}&code={code}";
				return Redirect(frontendFailUrl);
			}
		}
		else
		{
			return BadRequest(new { success = false, message = "Thiếu bookingId hoặc orderCode từ PayOS" });
		}
	}

    public class PaymentOfflineRequest
    {
        public int Amount { get; set; }
        public int? PaidByUserId { get; set; } // Optional - nếu không có thì tự động lấy từ booking customer
        public string Note { get; set; } = string.Empty;
    }

    [HttpPost("booking/{bookingId:int}/payments/offline")]
    [Authorize]
    public async Task<IActionResult> CreateOfflineForBooking([FromRoute] int bookingId, [FromBody] PaymentOfflineRequest req)
    {
        if (req == null || req.Amount <= 0)
        {
            return BadRequest(new { success = false, message = "amount là bắt buộc và phải lớn hơn 0" });
        }

        var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy booking" });
        }

        // Tự động lấy paidByUserId: ưu tiên từ request, nếu không có thì dùng customer ID từ booking (giống PayOS)
        var paidByUserId = req.PaidByUserId ?? booking.Customer?.User?.UserId ?? 0;
        if (paidByUserId <= 0)
        {
            return BadRequest(new { success = false, message = "Không thể xác định người thanh toán" });
        }

        Domain.Entities.Invoice? invoice = null;
        Domain.Entities.Payment? payment = null;

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                // Cập nhật booking status thành PAID (giống PayOS)
                booking.Status = BookingStatusConstants.Paid;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepo.UpdateBookingAsync(booking);

                // Tính toán amounts (giống PayOS)
                var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
                decimal packageDiscountAmount = 0m;
                decimal packagePrice = 0m;
                decimal promotionDiscountAmount = 0m;
                decimal partsAmount = (await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId))
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
                    .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

                if (booking.AppliedCreditId.HasValue)
                {
                    var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
                    if (appliedCredit?.ServicePackage != null)
                    {
                        packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
                        if (appliedCredit.UsedCredits == 0)
                        {
                            packagePrice = appliedCredit.ServicePackage.Price;
                        }
                    }
                }

                var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(booking.BookingId);
                if (userPromotions != null && userPromotions.Any())
                {
                    promotionDiscountAmount = userPromotions
                        .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                        .Sum(up => up.DiscountAmount);
                }

                var finalServicePrice = booking.AppliedCreditId.HasValue
                    ? (serviceBasePrice - packageDiscountAmount)
                    : serviceBasePrice;

                if (promotionDiscountAmount > finalServicePrice)
                {
                    promotionDiscountAmount = finalServicePrice;
                }

                decimal paymentAmount = packagePrice + finalServicePrice + partsAmount - promotionDiscountAmount;

                // Tạo hoặc cập nhật Invoice (giống PayOS)
                invoice = await _invoiceRepo.GetByBookingIdAsync(booking.BookingId);
                if (invoice == null)
                {
                    invoice = new Domain.Entities.Invoice
                    {
                        BookingId = booking.BookingId,
                        CustomerId = booking.CustomerId,
                        Email = booking.Customer?.User?.Email,
                        Phone = booking.Customer?.User?.PhoneNumber,
                        Status = PaymentConstants.InvoiceStatus.Paid,
                        PackageDiscountAmount = 0,
                        PromotionDiscountAmount = 0,
                        CreatedAt = DateTime.UtcNow,
                    };
                    invoice = await _invoiceRepo.CreateMinimalAsync(invoice);
                }
                else
                {
                    invoice.Status = PaymentConstants.InvoiceStatus.Paid;
                }

                // Cập nhật invoice amounts (giống PayOS)
                await _invoiceRepo.UpdateAmountsAsync(invoice.InvoiceId, packageDiscountAmount, promotionDiscountAmount, partsAmount);

                // Tạo Payment record (giống PayOS)
                payment = new Domain.Entities.Payment
                {
                    PaymentCode = $"PAYCASH{DateTime.UtcNow:yyyyMMddHHmmss}{bookingId}",
                    InvoiceId = invoice.InvoiceId,
                    PaymentMethod = "CASH",
                    Amount = (int)Math.Round(paymentAmount),
                    Status = PaymentConstants.PaymentStatus.Paid,
                    PaidAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    PaidByUserID = paidByUserId, // Dùng giá trị đã resolve (từ request hoặc booking customer)
                };

                payment = await _paymentRepo.CreateAsync(payment);

                // Xử lý applied credit đầy đủ (giống PayOS)
                if (booking.AppliedCreditId.HasValue)
                {
                    var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
                    if (appliedCredit != null)
                    {
                        appliedCredit.UsedCredits += 1;
                        appliedCredit.UpdatedAt = DateTime.UtcNow;

                        if (appliedCredit.UsedCredits >= appliedCredit.TotalCredits)
                        {
                            appliedCredit.Status = "USED_UP";
                        }

                        await _customerServiceCreditRepo.UpdateAsync(appliedCredit);
                    }
                }

                // Mark promotion as used (giống PayOS)
                try
                {
                    await _promotionService.MarkUsedByBookingAsync(booking.BookingId);
                }
                catch (Exception)
                {
                    // Ignore errors
                }

                scope.Complete();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi xử lý thanh toán offline: {ex.Message}" });
            }
        }

        // Gửi email hóa đơn và phiếu bảo dưỡng (giống PayOS)
        try
        {
            var customerEmail = booking.Customer?.User?.Email;
            if (!string.IsNullOrEmpty(customerEmail))
            {
                // Lấy thông tin phụ tùng phát sinh
                var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
                var parts = workOrderParts
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
                    .Select(p => new EVServiceCenter.Application.Service.InvoicePartItem
                    {
                        Name = p.Part?.PartName ?? $"Phụ tùng #{p.PartId}",
                        Quantity = p.QuantityUsed,
                        Amount = p.QuantityUsed * (p.Part?.Price ?? 0)
                    }).ToList();

                // Lấy thông tin promotion đã áp dụng
                var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(booking.BookingId);
                var promotions = userPromotions?
                    .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                    .Select(up => new EVServiceCenter.Application.Service.InvoicePromotionItem
                    {
                        Code = up.Promotion?.Code ?? "N/A",
                        Description = up.Promotion?.Description ?? "Khuyến mãi",
                        DiscountAmount = up.DiscountAmount
                    }).ToList() ?? new List<EVServiceCenter.Application.Service.InvoicePromotionItem>();

                var subject = $"Hóa đơn thanh toán - Booking #{booking.BookingId}";
                // Tính lại packageDiscountAmount trong scope này
                var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
                decimal packageDiscountAmount = 0m;
                if (booking.AppliedCreditId.HasValue)
                {
                    var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
                    if (appliedCredit?.ServicePackage != null)
                    {
                        packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
                    }
                }

                var body = await _emailService.RenderInvoiceEmailTemplateAsync(
                    booking.Customer?.User?.FullName ?? "Khách hàng",
                    $"INV-{booking.BookingId:D6}",
                    booking.BookingId.ToString(),
                    DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                    customerEmail,
                    booking.Service?.ServiceName ?? "N/A",
                    (booking.Service?.BasePrice ?? 0m).ToString("N0"),
                    payment.Amount.ToString("N0"),
                    booking.AppliedCreditId.HasValue,
                    packageDiscountAmount.ToString("N0")
                );

                var invoicePdfContent = await _pdfInvoiceService.GenerateInvoicePdfAsync(booking.BookingId);

                byte[]? maintenancePdfContent = null;
                try
                {
                    maintenancePdfContent = await _pdfInvoiceService.GenerateMaintenanceReportPdfAsync(booking.BookingId);
                }
                catch (Exception)
                {
                    // Ignore errors
                }

                if (maintenancePdfContent != null)
                {
                    var attachments = new List<(string fileName, byte[] content, string mimeType)>
                    {
                        ($"Invoice_Booking_{booking.BookingId}.pdf", invoicePdfContent, "application/pdf"),
                        ($"MaintenanceReport_Booking_{booking.BookingId}.pdf", maintenancePdfContent, "application/pdf")
                    };

                    await _emailService.SendEmailWithMultipleAttachmentsAsync(customerEmail, subject, body, attachments);
                }
                else
                {
                    await _emailService.SendEmailWithAttachmentAsync(
                        customerEmail,
                        subject,
                        body,
                        $"Invoice_Booking_{booking.BookingId}.pdf",
                        invoicePdfContent,
                        "application/pdf");
                }
            }
        }
        catch (Exception)
        {
            // Ignore email errors
        }

        // Gửi notification (giống PayOS)
        if (booking.Customer?.User?.UserId != null)
        {
            var uid = booking.Customer.User.UserId;
            await _notificationService.SendBookingNotificationAsync(uid, $"Đặt lịch #{booking.BookingId}", "Thanh toán thành công", "BOOKING");
        }

        return Ok(new {
            success = true,
            paymentId = payment?.PaymentId,
            paymentCode = payment?.PaymentCode,
            status = payment?.Status,
            amount = payment?.Amount,
            paymentMethod = payment?.PaymentMethod,
            paidByUserId = payment?.PaidByUserID
        });
    }



    [HttpGet("booking/{bookingId:int}/breakdown")]
    [Authorize]
    public async Task<IActionResult> GetBookingPaymentBreakdown([FromRoute] int bookingId)
    {
        try
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy booking" });
            }

            var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
            decimal packageDiscountAmount = 0m;
            decimal packagePrice = 0m;

            if (booking.AppliedCreditId.HasValue)
            {
                var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
                if (appliedCredit?.ServicePackage != null)
                {
                    packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
                    if (appliedCredit.UsedCredits == 0)
                    {
                        packagePrice = appliedCredit.ServicePackage.Price;
                    }
                }
            }

            var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
            var consumedParts = (workOrderParts ?? new List<Domain.Entities.WorkOrderPart>())
                .Where(p => string.Equals(p.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var inventoryParts = consumedParts.Where(p => !p.IsCustomerSupplied).ToList();
            var customerSuppliedParts = consumedParts.Where(p => p.IsCustomerSupplied && p.SourceOrderItemId.HasValue).ToList();

            var partsDetails = inventoryParts.Select(p => new
            {
                partId = p.PartId,
                name = p.Part?.PartName,
                qty = p.QuantityUsed,
                unitPrice = p.Part?.Price ?? 0m,
                amount = (p.Part?.Price ?? 0m) * p.QuantityUsed
            }).ToList();
            var partsAmount = partsDetails.Sum(x => x.amount);

            var orderItemIds = customerSuppliedParts.Select(p => p.SourceOrderItemId!.Value).Distinct().ToList();
            var refPrices = new Dictionary<int, decimal>();
            foreach (var id in orderItemIds)
            {
                var oi = await _orderRepository.GetOrderItemByIdAsync(id);
                if (oi != null) refPrices[id] = oi.UnitPrice;
            }
            var customerSuppliedDetails = customerSuppliedParts.Select(p => new
            {
                partId = p.PartId,
                name = p.Part?.PartName,
                qty = p.QuantityUsed,
                referenceUnitPrice = p.SourceOrderItemId.HasValue && refPrices.ContainsKey(p.SourceOrderItemId.Value) ? refPrices[p.SourceOrderItemId.Value] : 0m,
                amount = 0m,
                sourceOrderItemId = p.SourceOrderItemId
            }).ToList();

            decimal promotionDiscountAmount = 0m;
            var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
            if (userPromotions != null && userPromotions.Any())
            {
                promotionDiscountAmount = userPromotions
                    .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                    .Sum(up => up.DiscountAmount);
            }

            var finalServicePrice = booking.AppliedCreditId.HasValue
                ? (serviceBasePrice - packageDiscountAmount)
                : serviceBasePrice;

            if (promotionDiscountAmount > finalServicePrice)
            {
                promotionDiscountAmount = finalServicePrice;
            }

            var total = packagePrice + finalServicePrice + partsAmount - promotionDiscountAmount;

            var response = new
            {
                success = true,
                data = new
                {
                    bookingId = booking.BookingId,
                    service = new { name = booking.Service?.ServiceName, basePrice = serviceBasePrice },
                    package = new { applied = booking.AppliedCreditId.HasValue, firstTimePrice = packagePrice, discountAmount = packageDiscountAmount },
                    parts = new {
                        fromInventory = partsDetails,
                        fromCustomer = customerSuppliedDetails
                    },
                    partsAmount,
                    promotion = new { applied = promotionDiscountAmount > 0, discountAmount = promotionDiscountAmount },
                    subtotal = serviceBasePrice,
                    total,
                    notes = "Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói; phụ tùng không áp dụng khuyến mãi."
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi tạo breakdown thanh toán: {ex.Message}" });
        }
    }

	[HttpGet("/api/payment/cancel")]
	[AllowAnonymous]
    public async Task<IActionResult> Cancel([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = true)
	{
        try
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking != null && booking.Status == BookingStatusConstants.Completed)
            {
                var invoice = await _invoiceRepo.GetByBookingIdAsync(booking.BookingId);
                if (invoice != null && !string.Equals(invoice.Status, PaymentConstants.InvoiceStatus.Paid, StringComparison.OrdinalIgnoreCase))
                {
                    await _invoiceRepo.UpdateStatusAsync(invoice.InvoiceId, PaymentConstants.InvoiceStatus.Cancelled);
                }
            }
        }
        catch { /* swallow to not block redirect */ }

		var frontendUrl = _configuration["App:FrontendUrl"];
		var cancelledPath = _configuration["App:PaymentRedirects:CancelledPath"];
		var frontendCancelUrl = $"{frontendUrl}{cancelledPath}?bookingId={bookingId}&status={status}&code={code}";
		return Redirect(frontendCancelUrl);
	}

    [HttpPost("/api/payment/sepay-webhook")]
	[AllowAnonymous]
	public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookRequest request)
	{
		try
		{
			if (request == null)
			{
				return BadRequest(new { success = false, message = "Invalid webhook payload" });
			}

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
				var descriptionText = request.Description;

				var payIndex = descriptionText.IndexOf("Pay", StringComparison.OrdinalIgnoreCase);
				if (payIndex >= 0)
				{
					var mentIndex = descriptionText.IndexOf("ment", payIndex + 3, StringComparison.OrdinalIgnoreCase);
					if (mentIndex > payIndex + 3)
					{
						var bookingIdStr = descriptionText.Substring(payIndex + 3, mentIndex - payIndex - 3);
						if (int.TryParse(bookingIdStr, out var extractedBookingId))
						{
							bookingId = extractedBookingId;
						}
					}
				}
			}
			else if (!string.IsNullOrEmpty(request.TransactionId))
			{
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
				return BadRequest(new { success = false, message = "Cannot extract bookingId from webhook payload" });
			}

			var paymentStatus = request.Status?.ToUpperInvariant() ?? "";
			var description = request.Description ?? "";

			var isPaymentSuccess = paymentStatus == "SUCCESS"
				|| paymentStatus == "PAID"
				|| paymentStatus == "COMPLETED"
				|| paymentStatus == "00"
				|| (request.Code == "00")
				|| description.Contains("BankAPINotify", StringComparison.OrdinalIgnoreCase);

			if (!isPaymentSuccess)
			{
				return Ok(new { success = true, message = "Webhook received but payment not successful" });
			}

			var confirmed = await _paymentService.ConfirmPaymentAsync(bookingId.Value, "SEPAY");

			if (confirmed)
			{
				return Ok(new { success = true, message = "Payment confirmed successfully", bookingId = bookingId.Value });
			}
			else
			{
				return Ok(new { success = false, message = "Failed to confirm payment", bookingId = bookingId.Value });
			}
		}
		catch (Exception)
		{
			return StatusCode(500, new { success = false, message = "Internal server error processing webhook" });
		}
	}

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

	[HttpGet("admin")]
	[Authorize(Roles = "ADMIN,STAFF")]
	public async Task<IActionResult> ListForAdmin(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10,
		[FromQuery] int? customerId = null,
		[FromQuery] int? invoiceId = null,
		[FromQuery] int? bookingId = null,
		[FromQuery] int? orderId = null,
		[FromQuery] string? status = null,
		[FromQuery] string? paymentMethod = null,
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null,
		[FromQuery] string? searchTerm = null,
		[FromQuery] string sortBy = "createdAt",
		[FromQuery] string sortOrder = "desc")
	{
		if (!ModelState.IsValid)
		{
			var errors = ModelState
				.Where(x => x.Value?.Errors.Count > 0)
				.SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
				.ToList();
			return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
		}

		try
		{
			if (page < 1) return BadRequest(new { success = false, message = "Page phải lớn hơn 0" });
			if (pageSize < 1 || pageSize > 100) return BadRequest(new { success = false, message = "Page size phải từ 1 đến 100" });

			var (items, totalCount) = await _paymentRepo.QueryForAdminAsync(
				page, pageSize, customerId, invoiceId, bookingId, orderId, status, paymentMethod, from, to, searchTerm, sortBy, sortOrder);

			var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

			var data = items.Select(p => new
			{
				paymentId = p.PaymentId,
				paymentCode = p.PaymentCode,
				invoiceId = p.InvoiceId,
				amount = p.Amount,
				status = p.Status,
				paymentMethod = p.PaymentMethod,
				paidByUserId = p.PaidByUserID,
				createdAt = p.CreatedAt,
				paidAt = p.PaidAt,
				invoice = p.Invoice != null ? new
				{
					invoiceId = p.Invoice.InvoiceId,
					customerId = p.Invoice.CustomerId,
					bookingId = p.Invoice.BookingId,
					orderId = p.Invoice.OrderId,
					status = p.Invoice.Status,
					customer = p.Invoice.Customer != null ? new
					{
						customerId = p.Invoice.Customer.CustomerId,
						user = p.Invoice.Customer.User != null ? new
						{
							userId = p.Invoice.Customer.User.UserId,
							fullName = p.Invoice.Customer.User.FullName,
							email = p.Invoice.Customer.User.Email,
							phoneNumber = p.Invoice.Customer.User.PhoneNumber
						} : null
					} : null,
					booking = p.Invoice.Booking != null ? new
					{
						bookingId = p.Invoice.Booking.BookingId,
						serviceId = p.Invoice.Booking.ServiceId,
						centerId = p.Invoice.Booking.CenterId
					} : null,
					order = p.Invoice.Order != null ? new
					{
						orderId = p.Invoice.Order.OrderId,
						payOSOrderCode = p.Invoice.Order.PayOSOrderCode
					} : null
				} : null
			}).ToList();

			var pagination = new
			{
				CurrentPage = page,
				PageSize = pageSize,
				TotalItems = totalCount,
				TotalPages = totalPages,
				HasNextPage = page < totalPages,
				HasPreviousPage = page > 1
			};

			return Ok(new { success = true, data = data, pagination });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách payments: {ex.Message}" });
		}
	}

	[HttpGet("admin/stats")]
	[Authorize(Roles = "ADMIN,STAFF")]
	public async Task<IActionResult> GetStats(
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null,
		[FromQuery] int? centerId = null)
	{
		try
		{
			var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
			var toDate = to ?? DateTime.UtcNow;

			var allPayments = await _paymentRepo.GetPaymentsByStatusesAndDateRangeAsync(
				new[] { "PAID", "COMPLETED", "PENDING", "FAILED", "CANCELLED" },
				fromDate,
				toDate);

			if (centerId.HasValue)
			{
				allPayments = allPayments.Where(p =>
					(p.Invoice?.Booking?.CenterId == centerId.Value) ||
					(p.Invoice?.Order?.FulfillmentCenterId == centerId.Value)
				).ToList();
			}

			var total = allPayments.Count;
			var totalAmount = allPayments.Sum(p => (long)p.Amount);
			var paid = allPayments.Count(p => p.Status == "PAID" || p.Status == "COMPLETED");
			var paidAmount = allPayments.Where(p => p.Status == "PAID" || p.Status == "COMPLETED").Sum(p => (long)p.Amount);
			var pending = allPayments.Count(p => p.Status == "PENDING");
			var failed = allPayments.Count(p => p.Status == "FAILED");
			var cancelled = allPayments.Count(p => p.Status == "CANCELLED");

			var byMethod = new Dictionary<string, (int count, long amount)>();
			foreach (var method in new[] { "PAYOS", "SEPAY", "CASH" })
			{
				var methodPayments = allPayments.Where(p => p.PaymentMethod == method).ToList();
				byMethod[method.ToLowerInvariant()] = (
					methodPayments.Count,
					methodPayments.Sum(p => (long)p.Amount)
				);
			}

			var byStatus = new Dictionary<string, (int count, long amount)>();
			foreach (var status in new[] { "PAID", "PENDING", "FAILED", "CANCELLED" })
			{
				var statusPayments = allPayments.Where(p => p.Status == status).ToList();
				byStatus[status.ToLowerInvariant()] = (
					statusPayments.Count,
					statusPayments.Sum(p => (long)p.Amount)
				);
			}

			var today = DateTime.UtcNow.Date;
			var todayPayments = allPayments.Where(p => p.PaidAt?.Date == today || p.CreatedAt.Date == today).ToList();
			var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
			var thisMonthPayments = allPayments.Where(p => (p.PaidAt ?? p.CreatedAt) >= thisMonth).ToList();
			var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
			var thisYearPayments = allPayments.Where(p => (p.PaidAt ?? p.CreatedAt) >= thisYear).ToList();

			var stats = new
			{
				total,
				totalAmount,
				paid,
				paidAmount,
				pending,
				failed,
				cancelled,
				byMethod = new
				{
					payos = new { count = byMethod.GetValueOrDefault("payos").count, amount = byMethod.GetValueOrDefault("payos").amount },
					sepay = new { count = byMethod.GetValueOrDefault("sepay").count, amount = byMethod.GetValueOrDefault("sepay").amount },
					cash = new { count = byMethod.GetValueOrDefault("cash").count, amount = byMethod.GetValueOrDefault("cash").amount }
				},
				byStatus = new
				{
					paid = new { count = byStatus.GetValueOrDefault("paid").count, amount = byStatus.GetValueOrDefault("paid").amount },
					pending = new { count = byStatus.GetValueOrDefault("pending").count, amount = byStatus.GetValueOrDefault("pending").amount },
					failed = new { count = byStatus.GetValueOrDefault("failed").count, amount = byStatus.GetValueOrDefault("failed").amount },
					cancelled = new { count = byStatus.GetValueOrDefault("cancelled").count, amount = byStatus.GetValueOrDefault("cancelled").amount }
				},
				today = new { count = todayPayments.Count, amount = todayPayments.Sum(p => (long)p.Amount) },
				thisMonth = new { count = thisMonthPayments.Count, amount = thisMonthPayments.Sum(p => (long)p.Amount) },
				thisYear = new { count = thisYearPayments.Count, amount = thisYearPayments.Sum(p => (long)p.Amount) }
			};

			return Ok(new { success = true, data = stats });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thống kê payments: {ex.Message}" });
		}
	}

	[HttpGet("{paymentId:int}")]
	[Authorize(Roles = "ADMIN,STAFF")]
	public async Task<IActionResult> GetById(int paymentId)
	{
		try
		{
			var payment = await _paymentRepo.GetByIdWithDetailsAsync(paymentId);
			if (payment == null)
			{
				return NotFound(new { success = false, message = "Không tìm thấy payment" });
			}

			var data = new
			{
				paymentId = payment.PaymentId,
				paymentCode = payment.PaymentCode,
				invoiceId = payment.InvoiceId,
				amount = payment.Amount,
				status = payment.Status,
				paymentMethod = payment.PaymentMethod,
				paidByUserId = payment.PaidByUserID,
				createdAt = payment.CreatedAt,
				paidAt = payment.PaidAt,
				invoice = payment.Invoice != null ? new
				{
					invoiceId = payment.Invoice.InvoiceId,
					customerId = payment.Invoice.CustomerId,
					bookingId = payment.Invoice.BookingId,
					orderId = payment.Invoice.OrderId,
					status = payment.Invoice.Status,
					email = payment.Invoice.Email,
					phone = payment.Invoice.Phone,
					packageDiscountAmount = payment.Invoice.PackageDiscountAmount,
					promotionDiscountAmount = payment.Invoice.PromotionDiscountAmount,
					partsAmount = payment.Invoice.PartsAmount,
					createdAt = payment.Invoice.CreatedAt,
					customer = payment.Invoice.Customer != null ? new
					{
						customerId = payment.Invoice.Customer.CustomerId,
						user = payment.Invoice.Customer.User != null ? new
						{
							userId = payment.Invoice.Customer.User.UserId,
							fullName = payment.Invoice.Customer.User.FullName,
							email = payment.Invoice.Customer.User.Email,
							phoneNumber = payment.Invoice.Customer.User.PhoneNumber
						} : null
					} : null,
					booking = payment.Invoice.Booking != null ? new
					{
						bookingId = payment.Invoice.Booking.BookingId,
						serviceId = payment.Invoice.Booking.ServiceId,
						centerId = payment.Invoice.Booking.CenterId,
						service = payment.Invoice.Booking.Service != null ? new
						{
							serviceId = payment.Invoice.Booking.Service.ServiceId,
							serviceName = payment.Invoice.Booking.Service.ServiceName,
							basePrice = payment.Invoice.Booking.Service.BasePrice
						} : null
					} : null,
					order = payment.Invoice.Order != null ? new
					{
						orderId = payment.Invoice.Order.OrderId,
						payOSOrderCode = payment.Invoice.Order.PayOSOrderCode
					} : null,
					payments = payment.Invoice.Payments?.Select(p => new
					{
						paymentId = p.PaymentId,
						paymentCode = p.PaymentCode,
						amount = p.Amount,
						status = p.Status,
						paymentMethod = p.PaymentMethod,
						paidAt = p.PaidAt
					}).ToList()
				} : null
			};

			return Ok(new { success = true, data });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết payment: {ex.Message}" });
		}
	}

}
