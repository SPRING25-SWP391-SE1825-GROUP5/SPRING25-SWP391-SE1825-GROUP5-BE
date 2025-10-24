using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Globalization;

namespace EVServiceCenter.Application.Service;

public class PaymentService
{
	private readonly HttpClient _httpClient;
	private readonly PayOsOptions _options;
    private readonly IBookingRepository _bookingRepository;
    // WorkOrderRepository removed - functionality merged into BookingRepository
    private readonly IOrderRepository _orderRepository;
	private readonly IInvoiceRepository _invoiceRepository;
	private readonly IPaymentRepository _paymentRepository;
    private readonly ITechnicianRepository _technicianRepository;
    private readonly IEmailService _emailService;
    private readonly IWorkOrderPartRepository _workOrderPartRepository;
    private readonly IMaintenanceChecklistRepository _checklistRepository;
    private readonly IMaintenanceChecklistResultRepository _checklistResultRepository;
    private readonly IPromotionService _promotionService;
    private readonly ILogger<PaymentService> _logger;
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepository;
    private readonly IPdfInvoiceService _pdfInvoiceService;

    private readonly EVServiceCenter.Application.Interfaces.IHoldStore _holdStore;

    public PaymentService(HttpClient httpClient, IOptions<PayOsOptions> options, IBookingRepository bookingRepository, IOrderRepository orderRepository, IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, ITechnicianRepository technicianRepository, IEmailService emailService, IWorkOrderPartRepository workOrderPartRepository, IMaintenanceChecklistRepository checklistRepository, IMaintenanceChecklistResultRepository checklistResultRepository, EVServiceCenter.Application.Interfaces.IHoldStore holdStore, IPromotionService promotionService, ILogger<PaymentService> logger, ICustomerServiceCreditRepository customerServiceCreditRepository, IPdfInvoiceService pdfInvoiceService)
	{
		_httpClient = httpClient;
		_options = options.Value;
		_bookingRepository = bookingRepository;
		// WorkOrderRepository removed - functionality merged into BookingRepository
        _orderRepository = orderRepository;
		_invoiceRepository = invoiceRepository;
		_paymentRepository = paymentRepository;
        _technicianRepository = technicianRepository;
        _emailService = emailService;
        _workOrderPartRepository = workOrderPartRepository;
        _checklistRepository = checklistRepository;
        _checklistResultRepository = checklistResultRepository;
        _holdStore = holdStore;
        _promotionService = promotionService;
        _logger = logger;
        _customerServiceCreditRepository = customerServiceCreditRepository;
        _pdfInvoiceService = pdfInvoiceService;
        }

	public async Task<string?> CreateBookingPaymentLinkAsync(int bookingId)
	{
		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking == null) throw new InvalidOperationException("Booking không tồn tại");
		if (booking.Status == "CANCELLED") throw new InvalidOperationException("Booking đã bị hủy");

        // Tính tổng tiền theo logic: (gói hoặc dịch vụ lẻ) + parts - promotion
        var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
        decimal packageDiscountAmount = 0m;
        decimal promotionDiscountAmount = 0m; // TODO: tích hợp khi có khuyến mãi
        decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
            .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

        if (booking.AppliedCreditId.HasValue)
        {
            var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
            if (appliedCredit?.ServicePackage != null)
            {
                packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
            }
        }

        // total = (dùng gói: packageDiscountAmount; dùng lẻ: serviceBasePrice) - promotion + parts
        decimal totalAmount = (booking.AppliedCreditId.HasValue ? packageDiscountAmount : serviceBasePrice)
                               - promotionDiscountAmount + partsAmount;
        
        var amount = (int)Math.Round(totalAmount); // VNĐ integer
        if (amount < _options.MinAmount) amount = _options.MinAmount;

		var orderCode = booking.BookingId; // PayOS yêu cầu là số
        var rawDesc = $"Booking #{booking.BookingId}";
        var description = rawDesc.Length > _options.DescriptionMaxLength ? rawDesc.Substring(0, _options.DescriptionMaxLength) : rawDesc;

		var returnUrl = (_options.ReturnUrl ?? string.Empty);
		var cancelUrl = (_options.CancelUrl ?? string.Empty);

		// Chuẩn hóa canonical theo thứ tự khóa cố định, không encode, dùng invariant formatting
		var canonical = string.Create(CultureInfo.InvariantCulture, $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
		var signature = ComputeHmacSha256Hex(canonical, _options.ChecksumKey);

		var payload = new
		{
			orderCode,
			amount,
			description,
			items = new[]
			{
                new { name = $"Booking #{booking.BookingId}", quantity = 1, price = amount }
			},
			returnUrl,
			cancelUrl,
			signature
		};

		var createUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests";
		using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(createUrl));
		request.Headers.Add("x-client-id", _options.ClientId);
		request.Headers.Add("x-api-key", _options.ApiKey);
		request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

		var response = await _httpClient.SendAsync(request);
		var responseText = await response.Content.ReadAsStringAsync();
		
		// Xử lý trường hợp "Đơn thanh toán đã tồn tại"
		if (!response.IsSuccessStatusCode)
		{
			var errorJson = JsonDocument.Parse(responseText).RootElement;
			var code = errorJson.TryGetProperty("code", out var codeElem) ? codeElem.GetString() : null;
			var desc = errorJson.TryGetProperty("desc", out var errorDescElem) ? errorDescElem.GetString() : null;
			
			if (code == "231" && desc?.Contains("Đơn thanh toán đã tồn tại") == true)
			{
				// Lấy link cũ từ PayOS
				var existingUrl = await GetExistingPaymentLinkAsync(orderCode);
				if (!string.IsNullOrEmpty(existingUrl))
				{
					return existingUrl;
				}
				// Nếu không lấy được link cũ, tiếp tục throw exception
			}
			
		response.EnsureSuccessStatusCode();
		}
		
        var json = JsonDocument.Parse(responseText).RootElement;
        if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object &&
            dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
        {
            return urlElem.GetString();
        }

        var message =
            (json.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String ? msgElem.GetString() : null)
            ?? (json.TryGetProperty("desc", out var descElem) && descElem.ValueKind == JsonValueKind.String ? descElem.GetString() : null)
            ?? "Không nhận được checkoutUrl từ PayOS";
        throw new InvalidOperationException($"Tạo link PayOS thất bại: {message}. Response: {responseText}");
    }

    /// <summary>
    /// Lấy payment link đã tồn tại từ PayOS
    /// </summary>
    private async Task<string?> GetExistingPaymentLinkAsync(int orderCode)
    {
        try
        {
            var getUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests/{orderCode}";
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(getUrl));
            request.Headers.Add("x-client-id", _options.ClientId);
            request.Headers.Add("x-api-key", _options.ApiKey);

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Không thể lấy payment link cũ cho orderCode {OrderCode}: {ResponseText}", orderCode, responseText);
                return null;
            }
            
            var json = JsonDocument.Parse(responseText).RootElement;
            if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object &&
                dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
            {
                return urlElem.GetString();
            }
            
            _logger.LogWarning("Không tìm thấy checkoutUrl trong response cho orderCode {OrderCode}: {ResponseText}", orderCode, responseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy payment link cũ cho orderCode {OrderCode}", orderCode);
            return null;
        }
    }

    public async Task<string?> CreateOrderPaymentLinkAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) throw new InvalidOperationException("Đơn hàng không tồn tại");

        var amountDecimal = order.OrderItems?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
        var amount = (int)Math.Round(amountDecimal);
        if (amount < _options.MinAmount) amount = _options.MinAmount;

        var orderCode = orderId; // PayOS yêu cầu số
        var rawDesc = $"Order #{order.OrderId}";
        var description = rawDesc.Length > _options.DescriptionMaxLength ? rawDesc.Substring(0, _options.DescriptionMaxLength) : rawDesc;

        var returnUrl = (_options.ReturnUrl ?? string.Empty);
        var cancelUrl = (_options.CancelUrl ?? string.Empty);

        var canonical = string.Create(CultureInfo.InvariantCulture, $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
        var signature = ComputeHmacSha256Hex(canonical, _options.ChecksumKey);

        var items = (order.OrderItems ?? new List<Domain.Entities.OrderItem>())
            .Select(i => new { name = i.Part?.PartName ?? $"Part {i.PartId}", quantity = i.Quantity, price = (int)Math.Round(i.UnitPrice) })
            .DefaultIfEmpty(new { name = $"Order #{order.OrderId}", quantity = 1, price = amount })
            .ToArray();

        var payload = new
        {
            orderCode,
            amount,
            description,
            items,
            returnUrl,
            cancelUrl,
            signature
        };

        var createUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(createUrl));
        request.Headers.Add("x-client-id", _options.ClientId);
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(responseText).RootElement;
        if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object &&
            dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
        {
            return urlElem.GetString();
    }
		var message =
			(json.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String ? msgElem.GetString() : null)
			?? (json.TryGetProperty("desc", out var descElem) && descElem.ValueKind == JsonValueKind.String ? descElem.GetString() : null)
			?? "Không nhận được checkoutUrl từ PayOS";
		throw new InvalidOperationException($"Tạo link PayOS thất bại: {message}. Response: {responseText}");
	}

	private static string ComputeHmacSha256Hex(string data, string key)
	{
		if (string.IsNullOrEmpty(key)) return string.Empty;
		using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
		var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
		var sb = new StringBuilder(hashBytes.Length * 2);
		for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
		return sb.ToString();
	}

	public async Task<bool> ConfirmPaymentAsync(int bookingId)
	{
		if (bookingId <= 0) 
		{
			_logger.LogWarning("ConfirmPaymentAsync called with invalid bookingId: {BookingId}", bookingId);
			return false;
		}

		_logger.LogInformation("=== BẮT ĐẦU CONFIRM PAYMENT ===");
		_logger.LogInformation("ConfirmPaymentAsync called with bookingId: {BookingId}", bookingId);

		// Không cần gọi PayOS API nữa vì đã xác nhận từ ReturnUrl parameters
		_logger.LogInformation("Bỏ qua PayOS API call vì đã xác nhận từ ReturnUrl parameters");

		// Tìm booking trực tiếp bằng bookingId
		_logger.LogInformation("Tìm booking với ID: {BookingId}", bookingId);
		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking != null)
		{
			_logger.LogInformation("Tìm thấy booking {BookingId} với status hiện tại: {CurrentStatus}", bookingId, booking.Status);
		}
		else
		{
			_logger.LogWarning("Không tìm thấy booking với ID: {BookingId}", bookingId);
		}

		if (booking != null)
		{
			_logger.LogInformation("=== BẮT ĐẦU CẬP NHẬT DATABASE ===");
			_logger.LogInformation("Thanh toán thành công cho booking {BookingId}, đang cập nhật database...", booking.BookingId);
			// Cập nhật trạng thái booking
			_logger.LogInformation("Cập nhật booking {BookingId} từ {OldStatus} thành PAID", booking.BookingId, booking.Status);
			booking.Status = "PAID";
			booking.UpdatedAt = DateTime.UtcNow;
			await _bookingRepository.UpdateBookingAsync(booking);
			_logger.LogInformation("Đã cập nhật booking {BookingId} thành PAID", booking.BookingId);

			// Tạo hoặc cập nhật invoice
			_logger.LogInformation("Tạo/cập nhật invoice cho booking {BookingId}", booking.BookingId);
			var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
			if (invoice == null)
			{
				_logger.LogInformation("Tạo invoice mới cho booking {BookingId}", booking.BookingId);
				invoice = new Domain.Entities.Invoice
				{
					BookingId = booking.BookingId,
					CustomerId = booking.CustomerId,
					Email = booking.Customer?.User?.Email,
					Phone = booking.Customer?.User?.PhoneNumber,
					Status = "PAID",
					PackageDiscountAmount = 0,
					PromotionDiscountAmount = 0,
					CreatedAt = DateTime.UtcNow
				};
				await _invoiceRepository.CreateMinimalAsync(invoice);
				_logger.LogInformation("Đã tạo invoice {InvoiceId} cho booking {BookingId}", invoice.InvoiceId, booking.BookingId);
			}
			else
			{
				_logger.LogInformation("Cập nhật invoice {InvoiceId} cho booking {BookingId}", invoice.InvoiceId, booking.BookingId);
				invoice.Status = "PAID";
				// Note: IInvoiceRepository doesn't have UpdateAsync method
				// Invoice will be updated when booking is updated
			}

            // Tính các khoản theo logic mới
            var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
            decimal packageDiscountAmount = 0m;
            decimal promotionDiscountAmount = 0m; // hook khuyến mãi sau
            decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
                .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

            if (booking.AppliedCreditId.HasValue)
            {
                var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
                if (appliedCredit?.ServicePackage != null)
                {
                    packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
                }
            }

            decimal paymentAmount = (booking.AppliedCreditId.HasValue ? packageDiscountAmount : serviceBasePrice)
                                      - promotionDiscountAmount + partsAmount;

			// Tạo payment record
			_logger.LogInformation("Tạo payment record cho booking {BookingId} với amount {Amount}", booking.BookingId, paymentAmount);
            var payment = new Domain.Entities.Payment
			{
				InvoiceId = invoice.InvoiceId,
				Amount = (int)Math.Round(paymentAmount),
				PaymentMethod = "PAYOS",
				Status = "COMPLETED",
				PaymentCode = bookingId.ToString(),
				CreatedAt = DateTime.UtcNow,
				PaidAt = DateTime.UtcNow
			};
			await _paymentRepository.CreateAsync(payment);
			_logger.LogInformation("Đã tạo payment {PaymentId} cho booking {BookingId}", payment.PaymentId, booking.BookingId);

            // Cập nhật số liệu vào invoice
            await _invoiceRepository.UpdateAmountsAsync(invoice.InvoiceId, packageDiscountAmount, promotionDiscountAmount, partsAmount);

			// CustomerServiceCredit đã được tạo với status ACTIVE khi tạo booking
			// Trừ credit khi thanh toán thành công
			if (booking.AppliedCreditId.HasValue)
			{
				var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
				if (appliedCredit != null)
				{
					// Trừ 1 credit khi sử dụng dịch vụ
					appliedCredit.UsedCredits += 1;
					appliedCredit.UpdatedAt = DateTime.UtcNow;
					
					// Cập nhật status nếu hết credit
					if (appliedCredit.UsedCredits >= appliedCredit.TotalCredits)
					{
						appliedCredit.Status = "USED_UP";
					}
					
					await _customerServiceCreditRepository.UpdateAsync(appliedCredit);
					_logger.LogInformation("Used 1 credit from CustomerServiceCredit {CreditId} for customer {CustomerId}, package {PackageId}. UsedCredits: {UsedCredits}/{TotalCredits}", 
						appliedCredit.CreditId, booking.CustomerId, appliedCredit.PackageId, appliedCredit.UsedCredits, appliedCredit.TotalCredits);
				}
			}

			// Gửi email invoice PDF với template
			try
			{
				_logger.LogInformation("=== BẮT ĐẦU GỬI EMAIL INVOICE ===");
				var customerEmail = booking.Customer?.User?.Email;
				if (!string.IsNullOrEmpty(customerEmail))
				{
					_logger.LogInformation("Customer email found: {Email} for booking {BookingId}", customerEmail, booking.BookingId);
					var subject = $"Hóa đơn thanh toán - Booking #{booking.BookingId}";
					
					_logger.LogInformation("Rendering email template for booking {BookingId}", booking.BookingId);
					// Sử dụng template email thay vì hardcode
					var body = await _emailService.RenderInvoiceEmailTemplateAsync(
						customerName: booking.Customer?.User?.FullName ?? "Khách hàng",
						invoiceId: $"INV-{booking.BookingId:D6}",
						bookingId: booking.BookingId.ToString(),
						createdDate: DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
						customerEmail: customerEmail,
						serviceName: booking.Service?.ServiceName ?? "N/A",
						servicePrice: (booking.Service?.BasePrice ?? 0m).ToString("N0"),
						totalAmount: payment.Amount.ToString("N0"),
						hasDiscount: booking.AppliedCreditId.HasValue,
						discountAmount: booking.AppliedCreditId.HasValue ? (payment.Amount * 0.1m).ToString("N0") : "0"
					);
					_logger.LogInformation("Email template rendered successfully for booking {BookingId}", booking.BookingId);
					
					_logger.LogInformation("Generating PDF invoice for booking {BookingId}", booking.BookingId);
        // Generate PDF invoice content
        var invoicePdfContent = await _pdfInvoiceService.GenerateInvoicePdfAsync(booking.BookingId);
        _logger.LogInformation("PDF invoice generated successfully for booking {BookingId}, size: {Size} bytes", booking.BookingId, invoicePdfContent.Length);
        
        // Generate PDF maintenance report content (if available)
        byte[]? maintenancePdfContent = null;
        try
        {
            _logger.LogInformation("Generating maintenance report PDF for booking {BookingId}", booking.BookingId);
            maintenancePdfContent = await _pdfInvoiceService.GenerateMaintenanceReportPdfAsync(booking.BookingId);
            _logger.LogInformation("Maintenance report PDF generated successfully for booking {BookingId}, size: {Size} bytes", booking.BookingId, maintenancePdfContent?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not generate maintenance report PDF for booking {BookingId}", booking.BookingId);
        }
        
        // Send email with attachments
        if (maintenancePdfContent != null)
        {
            _logger.LogInformation("Sending email with both attachments for booking {BookingId}", booking.BookingId);
            // Send with both attachments
            var attachments = new List<(string fileName, byte[] content, string mimeType)>
            {
                ($"Invoice_Booking_{booking.BookingId}.pdf", invoicePdfContent, "application/pdf"),
                ($"MaintenanceReport_Booking_{booking.BookingId}.pdf", maintenancePdfContent, "application/pdf")
            };
            
            await _emailService.SendEmailWithMultipleAttachmentsAsync(
                customerEmail, 
                subject, 
                body, 
                attachments);
            _logger.LogInformation("Email with multiple attachments sent successfully for booking {BookingId}", booking.BookingId);
        }
        else
        {
            _logger.LogInformation("Sending email with invoice attachment only for booking {BookingId}", booking.BookingId);
            // Send with invoice only
            await _emailService.SendEmailWithAttachmentAsync(
                customerEmail, 
                subject, 
                body, 
                $"Invoice_Booking_{booking.BookingId}.pdf", 
                invoicePdfContent, 
                "application/pdf");
            _logger.LogInformation("Email with invoice attachment sent successfully for booking {BookingId}", booking.BookingId);
        }
					
					_logger.LogInformation("=== EMAIL INVOICE SENT SUCCESSFULLY ===");
					_logger.LogInformation("Invoice email sent for booking {BookingId} to {Email}", booking.BookingId, customerEmail);
				}
				else
				{
					_logger.LogWarning("Customer email is null or empty for booking {BookingId}", booking.BookingId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "=== ERROR SENDING INVOICE EMAIL ===");
				_logger.LogError(ex, "Error sending invoice email for booking {BookingId}", booking.BookingId);
			}

			_logger.LogInformation("=== HOÀN THÀNH CẬP NHẬT DATABASE ===");
			_logger.LogInformation("Payment confirmed for booking {BookingId}, invoice {InvoiceId}, payment {PaymentId}", booking.BookingId, invoice.InvoiceId, payment.PaymentId);
		return true;
	}
		else
		{
			_logger.LogInformation("Thanh toán chưa thành công hoặc không tìm thấy booking. Booking: {BookingFound}", booking != null ? "Found" : "Not Found");
		return false;
		}
	}
}


