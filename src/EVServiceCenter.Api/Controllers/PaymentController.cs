using System;
using System.Collections.Generic;
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
using EVServiceCenter.Application.Constants;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
	private readonly PaymentService _paymentService;
    private readonly IPayOSService _payOSService;
    private readonly IVNPayService _vnPayService;
    private readonly IBookingRepository _bookingRepo;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IWorkOrderPartRepository _workOrderPartRepo;
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepo;
    private readonly IPromotionRepository _promotionRepo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentService paymentService,
        IPayOSService payOSService,
        IVNPayService vnPayService,
        IBookingRepository bookingRepo,
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepo,
        IPaymentRepository paymentRepo,
        IWorkOrderPartRepository workOrderPartRepo,
        ICustomerServiceCreditRepository customerServiceCreditRepo,
        IPromotionRepository promotionRepo,
        IConfiguration configuration,
        ILogger<PaymentController> logger)
	{
		_paymentService = paymentService;
        _payOSService = payOSService;
        _vnPayService = vnPayService;
        _bookingRepo = bookingRepo;
        _orderRepository = orderRepository;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _workOrderPartRepo = workOrderPartRepo;
        _customerServiceCreditRepo = customerServiceCreditRepo;
        _promotionRepo = promotionRepo;
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

			if (booking.Status != BookingStatusConstants.Completed)
			{
				return BadRequest(new {
					success = false,
					message = $"Chỉ có thể tạo payment link khi booking đã hoàn thành ({BookingStatusConstants.Completed}). Trạng thái hiện tại: {booking.Status ?? "N/A"}"
				});
			}

            // Dùng service chuẩn để tính tổng tiền: dịch vụ/gói + parts CONSUMED - khuyến mãi
            // Service sẽ tự động xử lý trường hợp link đã tồn tại và lấy link hiện tại
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
			decimal packagePrice = 0m; // Giá mua gói (chỉ tính lần đầu)
			decimal partsAmount = 0m;
			decimal promotionDiscountAmount = 0m;

			// Tính parts amount: CHỈ tính phụ tùng KHÔNG phải khách cung cấp (IsCustomerSupplied = false)
			// Phụ tùng khách cung cấp (IsCustomerSupplied = true) đã được thanh toán khi mua Order, không tính lại
            var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
            if (workOrderParts != null && workOrderParts.Any())
            {
                partsAmount = workOrderParts
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied) // CHỈ tính phụ tùng không phải khách cung cấp
                    .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));
            }

			// Tính package discount và package price nếu có
			if (booking.AppliedCreditId.HasValue)
			{
				var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
				if (appliedCredit?.ServicePackage != null)
				{
					// Tính discount từ gói
					packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);

					// Lần đầu mua gói (UsedCredits == 0) → phải trả tiền mua gói
					// Lần sau dùng gói (UsedCredits > 0) → chỉ trả phần discount còn lại
					if (appliedCredit.UsedCredits == 0)
					{
						packagePrice = appliedCredit.ServicePackage.Price;
					}
				}
			}

			// Tính promotion discount
			var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
			if (userPromotions != null && userPromotions.Any())
			{
				promotionDiscountAmount = userPromotions
					.Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
					.Sum(up => up.DiscountAmount);
			}

            // Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói, không áp dụng cho parts
            // Tính giá dịch vụ sau khi trừ package discount
            var finalServicePrice = booking.AppliedCreditId.HasValue 
                ? (serviceBasePrice - packageDiscountAmount) // Nếu có package: giá dịch vụ sau khi trừ discount
                : serviceBasePrice; // Nếu không có package: giá dịch vụ gốc
            
            // Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói, không áp dụng cho parts
            if (promotionDiscountAmount > finalServicePrice)
            {
                promotionDiscountAmount = finalServicePrice;
            }
            // Total = packagePrice (nếu lần đầu) + finalServicePrice + parts - promotionDiscount
            decimal totalAmount = packagePrice + finalServicePrice + partsAmount - promotionDiscountAmount;

			var amount = (int)Math.Round(totalAmount);
			if (amount < EVServiceCenter.Application.Constants.AppConstants.PaymentAmounts.MinAmountVnd)
				amount = EVServiceCenter.Application.Constants.AppConstants.PaymentAmounts.MinAmountVnd;

			// Tạo transaction content (nội dung chuyển khoản)
			// Format: Pay{bookingId}ment để SePay có thể parse bookingId từ webhook
			var transactionContent = string.Format(EVServiceCenter.Application.Constants.AppConstants.TransactionContent.Format, bookingId);

			// Lấy cấu hình SePay từ appsettings
			var sepayAccount = _configuration["SePay:Account"] ?? SePayConstants.DefaultAccount;
			var sepayBank = _configuration["SePay:Bank"] ?? SePayConstants.DefaultBank;
			var sepayBeneficiary = _configuration["SePay:Beneficiary"] ?? SePayConstants.DefaultBeneficiary;
			var qrCodeBaseUrl = _configuration["SePay:QrCodeBaseUrl"] ?? SePayConstants.DefaultQrCodeBaseUrl;

			// Tạo QR code URL từ SePay
			// Format: https://qr.sepay.vn/img?acc={account}&bank={bank}&amount={amount}&des={description}
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
            // PayOSOrderCode là unique random number được lưu trong Booking/Order
            // Không còn dùng offset, tìm trực tiếp bằng PayOSOrderCode
            // Lấy PayOSOrderCode từ Booking để cancel
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
			// Tìm Booking hoặc Order dựa trên PayOSOrderCode
			// PayOSOrderCode là unique random number, không còn dùng offset
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
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error confirming order payment for order {OrderId}", actualOrderId);
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
                // Fallback: Nếu không tìm thấy Order, booking đã được tìm ở trên

                if (booking != null)
				{
                    if (payOSConfirmed)
                    {
                        try
                        {
                            confirmed = await _paymentService.ConfirmPaymentAsync(booking.BookingId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error confirming booking payment for booking {BookingId}", booking.BookingId);
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
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error confirming booking payment for booking {BookingId}", bookingId.Value);
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
                Status = PaymentConstants.InvoiceStatus.Paid,
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
            Status = PaymentConstants.PaymentStatus.Paid,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PaidByUserID = req.PaidByUserId,
        };

        payment = await _paymentRepo.CreateAsync(payment);
        return Ok(new { paymentId = payment.PaymentId, paymentCode = payment.PaymentCode, status = payment.Status, amount = payment.Amount, paymentMethod = payment.PaymentMethod, paidByUserId = payment.PaidByUserID });
    }



    /// <summary>
    /// Breakdown chi tiết số tiền cần thanh toán cho Booking: dịch vụ/gói, phụ tùng (đã tiêu thụ), khuyến mãi, tổng cộng
    /// </summary>
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

            // Dịch vụ/gói
            var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
            decimal packageDiscountAmount = 0m;
            decimal packagePrice = 0m; // Giá mua gói (chỉ lần đầu)

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

            // Phụ tùng phát sinh (đã tiêu thụ)
            var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
            var consumedParts = (workOrderParts ?? new List<Domain.Entities.WorkOrderPart>())
                .Where(p => string.Equals(p.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Tách 2 nhóm: lấy từ kho trung tâm vs phụ tùng do khách cung cấp
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

            // Load đơn giá tham chiếu từ OrderItem cho parts khách cung cấp
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
                amount = 0m, // không tính tiền hàng – khách tự cung cấp
                sourceOrderItemId = p.SourceOrderItemId
            }).ToList();

            // Khuyến mãi (chỉ áp dụng phần dịch vụ/gói)
            decimal promotionDiscountAmount = 0m;
            var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
            if (userPromotions != null && userPromotions.Any())
            {
                promotionDiscountAmount = userPromotions
                    .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                    .Sum(up => up.DiscountAmount);
            }

            // Không cho khuyến mãi vượt quá phần dịch vụ/gói
            // Tính giá dịch vụ sau khi trừ package discount
            var finalServicePrice = booking.AppliedCreditId.HasValue 
                ? (serviceBasePrice - packageDiscountAmount) // Nếu có package: giá dịch vụ sau khi trừ discount
                : serviceBasePrice; // Nếu không có package: giá dịch vụ gốc
            
            // Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói, không áp dụng cho parts
            if (promotionDiscountAmount > finalServicePrice)
            {
                promotionDiscountAmount = finalServicePrice;
            }

            // Tổng cộng
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
            _logger.LogError(ex, "Error building booking payment breakdown for {BookingId}", bookingId);
            return StatusCode(500, new { success = false, message = $"Lỗi tạo breakdown thanh toán: {ex.Message}" });
        }
    }

	[HttpGet("/api/payment/cancel")]
	[AllowAnonymous]
    public async Task<IActionResult> Cancel([FromQuery] int bookingId, [FromQuery] string? status = null, [FromQuery] string? code = null, [FromQuery] bool cancel = true)
	{
        // Ghi nhận hủy thanh toán (không đổi Booking.Status)
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

        // Redirect về trang hủy thanh toán trên FE
		var frontendUrl = _configuration["App:FrontendUrl"];
		var cancelledPath = _configuration["App:PaymentRedirects:CancelledPath"];
		var frontendCancelUrl = $"{frontendUrl}{cancelledPath}?bookingId={bookingId}&status={status}&code={code}";
		return Redirect(frontendCancelUrl);
	}

	/// <summary>
	/// Tạo VNPay payment link cho Booking
	/// </summary>
	[HttpPost("booking/{bookingId:int}/vnpay-link")]
	[Authorize]
	public async Task<IActionResult> CreateVNPayPaymentLink([FromRoute] int bookingId)
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
			decimal packagePrice = 0m; // Giá mua gói (chỉ tính lần đầu)
			decimal partsAmount = 0m;
			decimal promotionDiscountAmount = 0m;

			// Tính parts amount: CHỈ tính phụ tùng KHÔNG phải khách cung cấp (IsCustomerSupplied = false)
			// Phụ tùng khách cung cấp (IsCustomerSupplied = true) đã được thanh toán khi mua Order, không tính lại
            var workOrderParts = await _workOrderPartRepo.GetByBookingIdAsync(booking.BookingId);
            if (workOrderParts != null && workOrderParts.Any())
            {
                partsAmount = workOrderParts
                    .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied) // CHỈ tính phụ tùng không phải khách cung cấp
                    .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));
            }

			// Tính package discount và package price nếu có
			if (booking.AppliedCreditId.HasValue)
			{
				var appliedCredit = await _customerServiceCreditRepo.GetByIdAsync(booking.AppliedCreditId.Value);
				if (appliedCredit?.ServicePackage != null)
				{
					// Tính discount từ gói
					packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);

					// Lần đầu mua gói (UsedCredits == 0) → phải trả tiền mua gói
					// Lần sau dùng gói (UsedCredits > 0) → chỉ trả phần discount còn lại
					if (appliedCredit.UsedCredits == 0)
					{
						packagePrice = appliedCredit.ServicePackage.Price;
					}
				}
			}

			// Tính promotion discount
			var userPromotions = await _promotionRepo.GetUserPromotionsByBookingAsync(bookingId);
			if (userPromotions != null && userPromotions.Any())
			{
				promotionDiscountAmount = userPromotions
					.Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
					.Sum(up => up.DiscountAmount);
			}

            // Tính giá dịch vụ sau khi trừ package discount
            var finalServicePrice2 = booking.AppliedCreditId.HasValue 
                ? (serviceBasePrice - packageDiscountAmount) // Nếu có package: giá dịch vụ sau khi trừ discount
                : serviceBasePrice; // Nếu không có package: giá dịch vụ gốc
            
            // Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói, không áp dụng cho parts
            if (promotionDiscountAmount > finalServicePrice2)
            {
                promotionDiscountAmount = finalServicePrice2;
            }
            // Total = packagePrice (nếu lần đầu) + finalServicePrice + parts - promotionDiscount
            decimal totalAmount = packagePrice + finalServicePrice2 + partsAmount - promotionDiscountAmount;

			var amount = (decimal)Math.Round(totalAmount);
			var minAmount = _configuration.GetValue<decimal>("VNPay:MinAmount", 1000);
			if (amount < minAmount) amount = minAmount;

			var description = $"Thanh toán vé #{bookingId}";
			var customerName = booking.Customer?.User?.FullName ?? "Khách hàng";

			var paymentUrl = await _vnPayService.CreatePaymentUrlAsync(bookingId, amount, description, customerName);

			_logger.LogInformation("VNPay payment URL created for booking {BookingId}: {Url}", bookingId, paymentUrl);

			return Ok(new
			{
				success = true,
				message = "Tạo link thanh toán VNPay thành công",
				data = new { paymentUrl, bookingId, amount }
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating VNPay payment link for booking {BookingId}", bookingId);
			return StatusCode(500, new { success = false, message = $"Lỗi tạo link thanh toán VNPay: {ex.Message}" });
		}
	}

	/// <summary>
	/// VNPay Return URL - User redirect về sau khi thanh toán
	/// </summary>
	[HttpGet("/api/payment/vnpay-return")]
	[AllowAnonymous]
	public async Task<IActionResult> VNPayReturn([FromQuery] Dictionary<string, string> vnpayData)
	{
		try
		{
			_logger.LogInformation("=== VNPAY RETURN URL RECEIVED ===");
			_logger.LogInformation("VNPay Return Data: {Data}", System.Text.Json.JsonSerializer.Serialize(vnpayData));

			// Verify payment response
			var secureHash = vnpayData.ContainsKey("vnp_SecureHash") ? vnpayData["vnp_SecureHash"] : "";
			var isValid = _vnPayService.VerifyPaymentResponse(vnpayData, secureHash);

			if (!isValid)
			{
				_logger.LogWarning("VNPay return: Invalid payment signature");
				var frontendUrl = _configuration["App:FrontendUrl"];
				var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
				var frontendFailUrl = $"{frontendUrl}{failedPath}?error=invalid_signature";
				return Redirect(frontendFailUrl);
			}

			// Lấy bookingId từ response
			var bookingId = _vnPayService.GetBookingIdFromResponse(vnpayData);
			if (!bookingId.HasValue)
			{
				_logger.LogWarning("VNPay return: Cannot extract bookingId from response");
				var frontendUrl = _configuration["App:FrontendUrl"];
				var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
				var frontendFailUrl = $"{frontendUrl}{failedPath}?error=cannot_extract_booking";
				return Redirect(frontendFailUrl);
			}

			// Kiểm tra response code
			var responseCode = vnpayData.ContainsKey("vnp_ResponseCode") ? vnpayData["vnp_ResponseCode"] : "";
			var isPaymentSuccess = responseCode == AppConstants.PaymentResponseCodes.Success;

			if (isPaymentSuccess)
			{
				// Xác nhận thanh toán với payment method VNPAY
				_logger.LogInformation("VNPay return: Payment successful, confirming payment for booking {BookingId} with method VNPAY", bookingId.Value);
				var confirmed = await _paymentService.ConfirmPaymentAsync(bookingId.Value, "VNPAY");

				if (confirmed)
				{
					var frontendUrl = _configuration["App:FrontendUrl"];
					var successPath = _configuration["App:PaymentRedirects:SuccessPath"];
					var frontendSuccessUrl = $"{frontendUrl}{successPath}?bookingId={bookingId.Value}&status=success";
					return Redirect(frontendSuccessUrl);
				}
				else
				{
					var frontendUrl = _configuration["App:FrontendUrl"];
					var errorPath = _configuration["App:PaymentRedirects:ErrorPath"];
					var frontendErrorUrl = $"{frontendUrl}{errorPath}?bookingId={bookingId.Value}&error=system_error";
					return Redirect(frontendErrorUrl);
				}
			}
			else
			{
				_logger.LogInformation("VNPay return: Payment failed. ResponseCode: {Code}", responseCode);
				var frontendUrl = _configuration["App:FrontendUrl"];
				var failedPath = _configuration["App:PaymentRedirects:FailedPath"];
				var frontendFailUrl = $"{frontendUrl}{failedPath}?bookingId={bookingId.Value}&status=failed&code={responseCode}";
				return Redirect(frontendFailUrl);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing VNPay return URL");
			var frontendUrl = _configuration["App:FrontendUrl"];
			var errorPath = _configuration["App:PaymentRedirects:ErrorPath"];
			var frontendErrorUrl = $"{frontendUrl}{errorPath}?error=system_error";
			return Redirect(frontendErrorUrl);
		}
	}

	/// <summary>
	/// VNPay IPN (Instant Payment Notification) - Webhook từ VNPay
	/// </summary>
	[HttpPost("/api/payment/vnpay-ipn")]
	[AllowAnonymous]
	public async Task<IActionResult> VNPayIPN([FromForm] Dictionary<string, string> vnpayData)
	{
		try
		{
			_logger.LogInformation("=== VNPAY IPN RECEIVED ===");
			_logger.LogInformation("VNPay IPN Data: {Data}", System.Text.Json.JsonSerializer.Serialize(vnpayData));

			// Verify payment response
			var secureHash = vnpayData.ContainsKey("vnp_SecureHash") ? vnpayData["vnp_SecureHash"] : "";
			var isValid = _vnPayService.VerifyPaymentResponse(vnpayData, secureHash);

			if (!isValid)
			{
				_logger.LogWarning("VNPay IPN: Invalid payment signature");
				return StatusCode(200, new { RspCode = AppConstants.PaymentResponseCodes.InvalidSignature, Message = "Invalid signature" });
			}

			// Lấy bookingId từ response
			var bookingId = _vnPayService.GetBookingIdFromResponse(vnpayData);
			if (!bookingId.HasValue)
			{
				_logger.LogWarning("VNPay IPN: Cannot extract bookingId from response");
				return StatusCode(200, new { RspCode = AppConstants.PaymentResponseCodes.CannotExtractBookingId, Message = "Cannot extract bookingId" });
			}

			// Kiểm tra response code
			var responseCode = vnpayData.ContainsKey("vnp_ResponseCode") ? vnpayData["vnp_ResponseCode"] : "";
			var transactionStatus = vnpayData.ContainsKey("vnp_TransactionStatus") ? vnpayData["vnp_TransactionStatus"] : "";

			// VNPay: ResponseCode = "00" và TransactionStatus = "00" là thành công
			var isPaymentSuccess = responseCode == AppConstants.PaymentResponseCodes.Success && transactionStatus == AppConstants.PaymentResponseCodes.Success;

			if (isPaymentSuccess)
			{
				// Xác nhận thanh toán với payment method VNPAY
				_logger.LogInformation("VNPay IPN: Payment successful, confirming payment for booking {BookingId} with method VNPAY", bookingId.Value);
				var confirmed = await _paymentService.ConfirmPaymentAsync(bookingId.Value, "VNPAY");

				if (confirmed)
				{
					_logger.LogInformation("VNPay IPN: Payment confirmed successfully for booking {BookingId}", bookingId.Value);
					// VNPay yêu cầu trả về RspCode = "00" để báo đã xử lý thành công
					return StatusCode(200, new { RspCode = AppConstants.PaymentResponseCodes.Success, Message = "Confirm success" });
				}
				else
				{
					_logger.LogWarning("VNPay IPN: Failed to confirm payment for booking {BookingId}", bookingId.Value);
					return StatusCode(200, new { RspCode = AppConstants.PaymentResponseCodes.ConfirmFailed, Message = "Confirm failed" });
				}
			}
			else
			{
				_logger.LogInformation("VNPay IPN: Payment not successful. ResponseCode: {Code}, TransactionStatus: {Status}", responseCode, transactionStatus);
				// Vẫn trả về success để VNPay không retry
				return StatusCode(200, new { RspCode = AppConstants.PaymentResponseCodes.PaymentNotSuccessful, Message = "Payment not successful" });
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing VNPay IPN");
			// Trả về error để VNPay retry
			return StatusCode(200, new { RspCode = AppConstants.PaymentResponseCodes.InternalError, Message = "Internal error" });
		}
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
			// Validate request
			if (request == null)
			{
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
				// SePay có thể gửi Description với format: "BankAPINotify Pay157ment FT25308510006606..."
				// Cần tìm pattern "Pay{number}ment" trong Description
				var descriptionText = request.Description;

				// Tìm pattern "Pay" trong Description
				var payIndex = descriptionText.IndexOf("Pay", StringComparison.OrdinalIgnoreCase);
				if (payIndex >= 0)
				{
					// Tìm "ment" sau "Pay"
					var mentIndex = descriptionText.IndexOf("ment", payIndex + 3, StringComparison.OrdinalIgnoreCase);
					if (mentIndex > payIndex + 3)
					{
						// Extract số giữa "Pay" và "ment"
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
				return BadRequest(new { success = false, message = "Cannot extract bookingId from webhook payload" });
			}

			// Kiểm tra trạng thái thanh toán từ SePay
			// SePay gọi webhook khi thanh toán thành công, nếu có Description chứa "BankAPINotify" thì coi như thành công
			var paymentStatus = request.Status?.ToUpperInvariant() ?? "";
			var description = request.Description ?? "";

			// Check payment success từ nhiều điều kiện:
			// 1. Status có giá trị thành công
			// 2. Code = "00"
			// 3. Description chứa "BankAPINotify" (SePay gửi khi thanh toán thành công qua bank API)
			var isPaymentSuccess = paymentStatus == "SUCCESS"
				|| paymentStatus == "PAID"
				|| paymentStatus == "COMPLETED"
				|| paymentStatus == "00"
				|| (request.Code == "00")
				|| description.Contains("BankAPINotify", StringComparison.OrdinalIgnoreCase);

			if (!isPaymentSuccess)
			{
				// Trả về 200 OK để SePay không retry, nhưng không xử lý payment
				return Ok(new { success = true, message = "Webhook received but payment not successful" });
			}

			// Xác nhận thanh toán với payment method SEPAY
			var confirmed = await _paymentService.ConfirmPaymentAsync(bookingId.Value, "SEPAY");

			if (confirmed)
			{
				// Trả về HTTP 200-299 để SePay biết đã nhận được thành công
				return Ok(new { success = true, message = "Payment confirmed successfully", bookingId = bookingId.Value });
			}
			else
			{
				// Trả về 200 OK nhưng với success = false để SePay không retry
				// Nếu muốn SePay retry, có thể trả về 500
				return Ok(new { success = false, message = "Failed to confirm payment", bookingId = bookingId.Value });
			}
		}
		catch (Exception)
		{
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

	// ============================================
	// ADMIN ENDPOINTS
	// ============================================

	/// <summary>
	/// Admin: Lấy danh sách payments với pagination, filter, search, sort
	/// </summary>
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
		// Check ModelState for binding errors
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

			// Project to DTOs to avoid circular reference issues
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
			_logger.LogError(ex, "Error listing payments for admin");
			return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách payments: {ex.Message}" });
		}
	}

	/// <summary>
	/// Admin: Lấy thống kê payments
	/// </summary>
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

			// Get all payments in date range
			var allPayments = await _paymentRepo.GetPaymentsByStatusesAndDateRangeAsync(
				new[] { "PAID", "COMPLETED", "PENDING", "FAILED", "CANCELLED" },
				fromDate,
				toDate);

			// Filter by center if specified
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

			// By method
			var byMethod = new Dictionary<string, (int count, long amount)>();
			foreach (var method in new[] { "PAYOS", "VNPAY", "SEPAY", "CASH" })
			{
				var methodPayments = allPayments.Where(p => p.PaymentMethod == method).ToList();
				byMethod[method.ToLowerInvariant()] = (
					methodPayments.Count,
					methodPayments.Sum(p => (long)p.Amount)
				);
			}

			// By status
			var byStatus = new Dictionary<string, (int count, long amount)>();
			foreach (var status in new[] { "PAID", "PENDING", "FAILED", "CANCELLED" })
			{
				var statusPayments = allPayments.Where(p => p.Status == status).ToList();
				byStatus[status.ToLowerInvariant()] = (
					statusPayments.Count,
					statusPayments.Sum(p => (long)p.Amount)
				);
			}

			// Today, this month, this year
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
					vnpay = new { count = byMethod.GetValueOrDefault("vnpay").count, amount = byMethod.GetValueOrDefault("vnpay").amount },
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
			_logger.LogError(ex, "Error getting payment stats");
			return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thống kê payments: {ex.Message}" });
		}
	}

	/// <summary>
	/// Admin: Lấy chi tiết payment
	/// </summary>
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

			// Project to DTO to avoid circular reference
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
			_logger.LogError(ex, "Error getting payment by id {PaymentId}", paymentId);
			return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết payment: {ex.Message}" });
		}
	}

}
