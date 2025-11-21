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
using EVServiceCenter.Application.Constants;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Globalization;
using System.Transactions;

namespace EVServiceCenter.Application.Service;

public class PaymentService
{
	private readonly HttpClient _httpClient;
	private readonly PayOsOptions _options;
    private readonly IBookingRepository _bookingRepository;
    private readonly IOrderRepository _orderRepository;
	private readonly IInvoiceRepository _invoiceRepository;
	private readonly IPaymentRepository _paymentRepository;
    private readonly ITechnicianRepository _technicianRepository;
    private readonly IEmailService _emailService;
    private readonly IWorkOrderPartRepository _workOrderPartRepository;
    private readonly IMaintenanceChecklistRepository _checklistRepository;
    private readonly IMaintenanceChecklistResultRepository _checklistResultRepository;
    private readonly IPromotionService _promotionService;
    private readonly IPromotionRepository _promotionRepository;
    private readonly INotificationService _notificationService;
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepository;
    private readonly IPdfInvoiceService _pdfInvoiceService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICenterRepository _centerRepository;

    private readonly EVServiceCenter.Application.Interfaces.IHoldStore _holdStore;

    public PaymentService(HttpClient httpClient, IOptions<PayOsOptions> options, IBookingRepository bookingRepository, IOrderRepository orderRepository, IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, ITechnicianRepository technicianRepository, IEmailService emailService, IWorkOrderPartRepository workOrderPartRepository, IMaintenanceChecklistRepository checklistRepository, IMaintenanceChecklistResultRepository checklistResultRepository, EVServiceCenter.Application.Interfaces.IHoldStore holdStore, IPromotionService promotionService, IPromotionRepository promotionRepository, ICustomerServiceCreditRepository customerServiceCreditRepository, IPdfInvoiceService pdfInvoiceService, INotificationService notificationService, IInventoryRepository inventoryRepository, ICenterRepository centerRepository)
	{
		_httpClient = httpClient;
		_options = options.Value;
		_bookingRepository = bookingRepository;
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
        _promotionRepository = promotionRepository;
        _customerServiceCreditRepository = customerServiceCreditRepository;
        _pdfInvoiceService = pdfInvoiceService;
        _notificationService = notificationService;
        _inventoryRepository = inventoryRepository;
        _centerRepository = centerRepository;
        }

	public async Task<string?> CreateBookingPaymentLinkAsync(int bookingId, bool isRetry = false)
	{
		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking == null) throw new InvalidOperationException("Booking không tồn tại");
		if (booking.Status == BookingStatusConstants.Cancelled || booking.Status == BookingStatusConstants.Canceled)
			throw new InvalidOperationException("Booking đã bị hủy");
		if (booking.Status != BookingStatusConstants.Completed)
			throw new InvalidOperationException($"Chỉ có thể tạo payment link khi booking đã hoàn thành ({BookingStatusConstants.Completed}). Trạng thái hiện tại: " + (booking.Status ?? "N/A"));

        var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
        decimal packageDiscountAmount = 0m;
        decimal packagePrice = 0m;
        decimal promotionDiscountAmount = 0m;
        decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
            .Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
            .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

        if (booking.AppliedCreditId.HasValue)
        {
            var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
            if (appliedCredit?.ServicePackage != null)
            {
                packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);

                if (appliedCredit.UsedCredits == 0)
                {
                    packagePrice = appliedCredit.ServicePackage.Price;
                }
            }
        }

        var userPromotions = await _promotionRepository.GetUserPromotionsByBookingAsync(booking.BookingId);
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
        if (amount < _options.MinAmount) amount = _options.MinAmount;

		int orderCode;
		if (booking.PayOSOrderCode.HasValue)
		{
			orderCode = booking.PayOSOrderCode.Value;
		}
		else
		{
			orderCode = await GenerateUniquePayOSOrderCodeAsync();
			await _bookingRepository.UpdatePayOSOrderCodeAsync(bookingId, orderCode);
			booking.PayOSOrderCode = orderCode;
		}
        var rawDesc = $"Booking #{booking.BookingId}";
        var description = rawDesc.Length > _options.DescriptionMaxLength ? rawDesc.Substring(0, _options.DescriptionMaxLength) : rawDesc;

        var returnUrl = ($"{_options.ReturnUrl ?? string.Empty}?bookingId={booking.BookingId}");
        var cancelUrl = ($"{_options.CancelUrl ?? string.Empty}?bookingId={booking.BookingId}");

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

        var json = JsonDocument.Parse(responseText).RootElement;
        var code = json.TryGetProperty("code", out var codeElem) ? codeElem.GetString() : null;
        var desc = json.TryGetProperty("desc", out var descElem) ? descElem.GetString() : null;

        if (code == "231" && desc?.Contains("Đơn thanh toán đã tồn tại") == true)
        {
            if (json.TryGetProperty("data", out var errData) && errData.ValueKind == JsonValueKind.Object)
            {
                if (errData.TryGetProperty("checkoutUrl", out var errUrl) && errUrl.ValueKind == JsonValueKind.String)
                {
                    var reuseUrl = errUrl.GetString();
                    if (!string.IsNullOrEmpty(reuseUrl))
                    {
                        return reuseUrl;
                    }
                }
            }

            var existingUrl = await GetExistingPaymentLinkAsync(orderCode);
            if (!string.IsNullOrEmpty(existingUrl))
            {
                return existingUrl;
            }

            if (!isRetry)
            {
                var cancelled = await CancelPaymentLinkAsync(orderCode);
                if (cancelled)
                {
                    return await CreateBookingPaymentLinkAsync(bookingId, isRetry: true);
                }
            }

            throw new InvalidOperationException($"Đơn thanh toán đã tồn tại trên PayOS cho booking #{bookingId}, nhưng không thể lấy được payment link. Vui lòng liên hệ hỗ trợ hoặc thử lại sau.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var message =
                desc
                ?? (json.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String ? msgElem.GetString() : null)
                ?? "Không nhận được checkoutUrl từ PayOS";
            throw new InvalidOperationException($"Tạo link PayOS thất bại: {message}. Response: {responseText}");
        }

        if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object &&
            dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
        {
            return urlElem.GetString();
        }

        var errorMessage =
            desc
            ?? (json.TryGetProperty("message", out var msgElem2) && msgElem2.ValueKind == JsonValueKind.String ? msgElem2.GetString() : null)
            ?? "Không nhận được checkoutUrl từ PayOS";
        throw new InvalidOperationException($"Tạo link PayOS thất bại: {errorMessage}. Response: {responseText}");
    }

    public async Task<string?> GetExistingOrderPaymentLinkAsync(int orderId)
    {
        if (orderId <= 0)
        {
            throw new ArgumentException($"OrderId không hợp lệ: {orderId}. OrderId phải lớn hơn 0.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Không tìm thấy đơn hàng với ID: {orderId}. Vui lòng kiểm tra lại thông tin đơn hàng.");
        }

        if (string.IsNullOrWhiteSpace(order.Status))
        {
            throw new InvalidOperationException($"Đơn hàng #{orderId} không có trạng thái. Vui lòng liên hệ quản trị viên.");
        }

        var statusUpper = order.Status.ToUpperInvariant();
        if (statusUpper == PaymentConstants.OrderStatus.Cancelled || statusUpper == PaymentConstants.OrderStatus.Canceled)
        {
            throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Đơn hàng đã bị hủy (Status: {order.Status}).");
        }

        if (statusUpper == PaymentConstants.OrderStatus.Paid || statusUpper == PaymentConstants.OrderStatus.Completed)
        {
            throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Đơn hàng đã được thanh toán (Status: {order.Status}).");
        }

        if (order.OrderItems == null || !order.OrderItems.Any())
        {
            throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Đơn hàng không có sản phẩm nào.");
        }

        var totalAmount = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
        if (totalAmount <= 0)
        {
            throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Tổng tiền đơn hàng phải lớn hơn 0 (Hiện tại: {totalAmount:N0} VNĐ).");
        }

        if (order.Invoices != null && order.Invoices.Any())
        {
            var paidInvoice = order.Invoices.FirstOrDefault(i =>
                i.Status != null && (i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Paid || i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Completed));
            if (paidInvoice != null)
            {
                throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Đơn hàng đã có hóa đơn thanh toán (Invoice ID: {paidInvoice.InvoiceId}).");
            }
        }

        if (!order.PayOSOrderCode.HasValue)
        {
            throw new InvalidOperationException($"Order #{orderId} chưa có PayOSOrderCode. Vui lòng tạo payment link trước.");
        }
        return await GetExistingPaymentLinkAsync(order.PayOSOrderCode.Value);
    }

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
                return null;
            }

            var json = JsonDocument.Parse(responseText).RootElement;

            string? paymentStatus = null;
            if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object)
            {
                if (dataElem.TryGetProperty("status", out var statusElem) && statusElem.ValueKind == JsonValueKind.String)
                {
                    paymentStatus = statusElem.GetString();
                }

                if (dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
                {
                    var checkoutUrl = urlElem.GetString();
                    if (!string.IsNullOrEmpty(checkoutUrl))
                    {
                        return checkoutUrl;
                    }
                }
            }

            if (paymentStatus == PaymentConstants.PaymentStatus.Cancelled)
            {
                return null;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<bool> CancelPaymentLinkAsync(int orderCode)
    {
        try
        {
            var cancelUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests/{orderCode}";
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(cancelUrl));
            request.Headers.Add("x-client-id", _options.ClientId);
            request.Headers.Add("x-api-key", _options.ApiKey);

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<string?> CreateOrderPaymentLinkAsync(int orderId, string? cancelUrl = null)
    {
        if (orderId <= 0)
        {
            throw new ArgumentException($"OrderId không hợp lệ: {orderId}. OrderId phải lớn hơn 0.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Không tìm thấy đơn hàng với ID: {orderId}. Vui lòng kiểm tra lại thông tin đơn hàng.");
        }

        if (string.IsNullOrWhiteSpace(order.Status))
        {
            throw new InvalidOperationException($"Đơn hàng #{orderId} không có trạng thái. Vui lòng liên hệ quản trị viên.");
        }

        var statusUpper = order.Status.ToUpperInvariant();
        if (statusUpper == PaymentConstants.OrderStatus.Cancelled || statusUpper == PaymentConstants.OrderStatus.Canceled)
        {
            throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Đơn hàng đã bị hủy (Status: {order.Status}).");
        }

        if (statusUpper == PaymentConstants.OrderStatus.Paid || statusUpper == PaymentConstants.OrderStatus.Completed)
        {
            throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Đơn hàng đã được thanh toán (Status: {order.Status}).");
        }

        if (order.OrderItems == null || !order.OrderItems.Any())
        {
            throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Đơn hàng không có sản phẩm nào.");
        }

        var amountDecimal = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
        if (amountDecimal <= 0)
        {
            throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Tổng tiền đơn hàng phải lớn hơn 0 (Hiện tại: {amountDecimal:N0} VNĐ).");
        }

        var amount = (int)Math.Round(amountDecimal);
        if (amount < _options.MinAmount)
        {
            throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Tổng tiền ({amount:N0} VNĐ) nhỏ hơn số tiền tối thiểu được phép ({_options.MinAmount:N0} VNĐ).");
        }

        if (order.Invoices != null && order.Invoices.Any())
        {
            var paidInvoice = order.Invoices.FirstOrDefault(i =>
                i.Status != null && (i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Paid || i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Completed));
            if (paidInvoice != null)
            {
                throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Đơn hàng đã có hóa đơn thanh toán (Invoice ID: {paidInvoice.InvoiceId}).");
            }
        }

        int orderCode;
        if (order.PayOSOrderCode.HasValue)
        {
            orderCode = order.PayOSOrderCode.Value;
        }
        else
        {
            orderCode = await GenerateUniquePayOSOrderCodeAsync();
            await _orderRepository.UpdatePayOSOrderCodeAsync(orderId, orderCode);
            order.PayOSOrderCode = orderCode;
        }
        var rawDesc = $"Order #{order.OrderId}";
        var description = rawDesc.Length > _options.DescriptionMaxLength ? rawDesc.Substring(0, _options.DescriptionMaxLength) : rawDesc;

        var returnUrl = (_options.ReturnUrl ?? string.Empty);
        var finalCancelUrl = cancelUrl ?? (_options.CancelUrl ?? string.Empty);

        var canonical = string.Create(CultureInfo.InvariantCulture, $"amount={amount}&cancelUrl={finalCancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
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
            cancelUrl = finalCancelUrl,
            signature
        };

        var createUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(createUrl));
        request.Headers.Add("x-client-id", _options.ClientId);
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = JsonDocument.Parse(responseText).RootElement;
            var code = errorJson.TryGetProperty("code", out var codeElem) ? codeElem.GetString() : null;
            var desc = errorJson.TryGetProperty("desc", out var errorDescElem) ? errorDescElem.GetString() : null;

            if (code == PaymentConstants.PayOSErrorCodes.PaymentExists && desc?.Contains("Đơn thanh toán đã tồn tại") == true)
            {
                if (errorJson.TryGetProperty("data", out var errorDataElem) && errorDataElem.ValueKind == JsonValueKind.Object)
                {
                    if (errorDataElem.TryGetProperty("checkoutUrl", out var errorUrlElem) && errorUrlElem.ValueKind == JsonValueKind.String)
                    {
                        var errorCheckoutUrl = errorUrlElem.GetString();
                        if (!string.IsNullOrEmpty(errorCheckoutUrl))
                        {
                            return errorCheckoutUrl;
                        }
                    }
                }

                var existingUrl = await GetExistingPaymentLinkAsync(orderCode);
                if (!string.IsNullOrEmpty(existingUrl))
                {
                    return existingUrl;
                }

                throw new InvalidOperationException($"Đơn thanh toán đã tồn tại trên PayOS cho đơn hàng #{order.OrderId}, nhưng không thể lấy được payment link. Vui lòng liên hệ hỗ trợ hoặc thử lại sau.");
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

    private int GenerateUniquePayOSOrderCode()
    {
        var random = new Random();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var randomPart = random.Next(1000, 10000);
        var orderCodeStr = timestamp + randomPart.ToString();

        if (int.TryParse(orderCodeStr, out var orderCode) && orderCode > 0)
        {
            return orderCode;
        }

        var hash = Math.Abs((timestamp + randomPart).GetHashCode());
        return Math.Max(100000000, hash);
    }

    private async Task<int> GenerateUniquePayOSOrderCodeAsync(int maxRetries = 10)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var orderCode = GenerateUniquePayOSOrderCode();

            var bookingExists = await _bookingRepository.PayOSOrderCodeExistsAsync(orderCode);
            var orderExists = await _orderRepository.PayOSOrderCodeExistsAsync(orderCode);

            if (!bookingExists && !orderExists)
            {
                return orderCode;
            }

            await Task.Delay(10);
        }

        throw new InvalidOperationException("Không thể generate unique PayOSOrderCode sau nhiều lần thử");
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

	public async Task<bool> ConfirmPaymentAsync(int bookingId, string paymentMethod = "PAYOS")
	{
		if (bookingId <= 0)
		{
			return false;
		}

		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking == null)
		{
			return false;
		}

		Domain.Entities.Invoice? invoice = null;
		Domain.Entities.Payment? payment = null;

		using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
		{
			try
			{
				booking.Status = BookingStatusConstants.Paid;
				booking.UpdatedAt = DateTime.UtcNow;
				await _bookingRepository.UpdateBookingAsync(booking);

				invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
				if (invoice == null)
				{
					invoice = new Domain.Entities.Invoice
					{
						BookingId = booking.BookingId,
						CustomerId = booking.CustomerId,
						Email = booking.Customer?.User?.Email,
						Phone = booking.Customer?.User?.PhoneNumber,
						Status = PaymentConstants.PaymentStatus.Paid,
						PackageDiscountAmount = 0,
						PromotionDiscountAmount = 0,
						CreatedAt = DateTime.UtcNow
					};
					await _invoiceRepository.CreateMinimalAsync(invoice);
				}
				else
				{
					invoice.Status = PaymentConstants.InvoiceStatus.Paid;
				}

				var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
				decimal packageDiscountAmount = 0m;
				decimal packagePrice = 0m;
				decimal promotionDiscountAmount = 0m;
				decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
					.Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
					.Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

				if (booking.AppliedCreditId.HasValue)
				{
					var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
					if (appliedCredit?.ServicePackage != null)
					{
						packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
						if (appliedCredit.UsedCredits == 0)
						{
							packagePrice = appliedCredit.ServicePackage.Price;
						}
					}
				}

				var userPromotions = await _promotionRepository.GetUserPromotionsByBookingAsync(booking.BookingId);
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

				payment = new Domain.Entities.Payment
				{
					InvoiceId = invoice.InvoiceId,
					Amount = (int)Math.Round(paymentAmount),
					PaymentMethod = paymentMethod,
					Status = BookingStatusConstants.Completed,
					PaymentCode = bookingId.ToString(),
					CreatedAt = DateTime.UtcNow,
					PaidAt = DateTime.UtcNow,
					PaidByUserID = booking.Customer?.User?.UserId
				};
				await _paymentRepository.CreateAsync(payment);

				await _invoiceRepository.UpdateAmountsAsync(invoice.InvoiceId, packageDiscountAmount, promotionDiscountAmount, partsAmount);

				if (booking.AppliedCreditId.HasValue)
				{
					var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
					if (appliedCredit != null)
					{
						appliedCredit.UsedCredits += 1;
						appliedCredit.UpdatedAt = DateTime.UtcNow;

						if (appliedCredit.UsedCredits >= appliedCredit.TotalCredits)
						{
							appliedCredit.Status = "USED_UP";
						}

						await _customerServiceCreditRepository.UpdateAsync(appliedCredit);
					}
				}

				try
				{
					await _promotionService.MarkUsedByBookingAsync(booking.BookingId);
				}
				catch (Exception)
				{
				}

				scope.Complete();
			}
			catch
			{
				throw;
			}
		}

		// Gửi email trong background để không làm chậm redirect
		try
		{
			var customerEmail = booking.Customer?.User?.Email;
			if (!string.IsNullOrEmpty(customerEmail))
			{
				// Sử dụng Task.Run để chạy email processing trong background
				_ = Task.Run(async () =>
				{
					try
					{
						// Lấy thông tin phụ tùng phát sinh
						var workOrderParts = await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId);
						var parts = workOrderParts
							.Where(p => p.Status == "CONSUMED" && !p.IsCustomerSupplied)
							.Select(p => new EVServiceCenter.Application.Service.InvoicePartItem
							{
								Name = p.Part?.PartName ?? $"Phụ tùng #{p.PartId}",
								Quantity = p.QuantityUsed,
								Amount = p.QuantityUsed * (p.Part?.Price ?? 0)
							}).ToList();

						// Tính lại packageDiscountAmount trong scope này
						var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
						decimal packageDiscountAmount = 0m;
						if (booking.AppliedCreditId.HasValue)
						{
							var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
							if (appliedCredit?.ServicePackage != null)
							{
								packageDiscountAmount = serviceBasePrice * ((appliedCredit.ServicePackage.DiscountPercent ?? 0) / 100);
							}
						}

						// Lấy thông tin promotion đã áp dụng
						var userPromotions = await _promotionRepository.GetUserPromotionsByBookingAsync(booking.BookingId);
						var promotions = userPromotions?
							.Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
							.Select(up => new EVServiceCenter.Application.Service.InvoicePromotionItem
							{
								Code = up.Promotion?.Code ?? "N/A",
								Description = up.Promotion?.Description ?? "Khuyến mãi",
								DiscountAmount = up.DiscountAmount
							}).ToList() ?? new List<EVServiceCenter.Application.Service.InvoicePromotionItem>();

						var subject = $"Hóa đơn thanh toán - Booking #{booking.BookingId}";
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
					catch (Exception ex)
					{
						// Log error nhưng không throw để không crash background task
						System.Console.WriteLine($"Background email error for booking {booking.BookingId}: {ex.Message}");
					}
				});
			}
		}
		catch (Exception)
		{
			// Ignore email errors để không ảnh hưởng đến payment confirmation
		}

        if (booking.Customer?.User?.UserId != null)
        {
            var uid = booking.Customer.User.UserId;
            await _notificationService.SendBookingNotificationAsync(uid, $"Đặt lịch #{booking.BookingId}", "Thanh toán thành công", "BOOKING");
        }

		return true;
	}

	public async Task<bool> ConfirmOrderPaymentAsync(int orderId, string paymentMethod = "PAYOS")
	{
		if (orderId <= 0)
		{
			return false;
		}

		var order = await _orderRepository.GetByIdAsync(orderId);
		if (order == null)
		{
			return false;
		}

		if (order.Status == PaymentConstants.OrderStatus.Paid || order.Status == PaymentConstants.OrderStatus.Completed)
		{
			return true;
		}

		Domain.Entities.Invoice? invoice = null;
		Domain.Entities.Payment? payment = null;

		using (var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
		{
			try
			{
				order.Status = PaymentConstants.OrderStatus.Paid;
				order.UpdatedAt = DateTime.UtcNow;
				await _orderRepository.UpdateAsync(order);

				var existingInvoices = order.Invoices?.ToList() ?? new List<Domain.Entities.Invoice>();
				invoice = existingInvoices.FirstOrDefault();

				if (invoice == null)
				{
					invoice = new Domain.Entities.Invoice
					{
						OrderId = order.OrderId,
						CustomerId = order.CustomerId,
						Email = order.Customer?.User?.Email,
						Phone = order.Customer?.User?.PhoneNumber,
						Status = PaymentConstants.InvoiceStatus.Paid,
						CreatedAt = DateTime.UtcNow
					};
					invoice = await _invoiceRepository.CreateMinimalAsync(invoice);
				}
				else
				{
					invoice.Status = PaymentConstants.InvoiceStatus.Paid;
				}

				var partsAmount = order.OrderItems?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;

				int? fulfillmentCenterId = null;
				if (order.OrderItems != null && order.OrderItems.Any())
				{
					if (order.FulfillmentCenterId.HasValue)
					{
						fulfillmentCenterId = order.FulfillmentCenterId.Value;
						await ReserveInventoryForOrderAsync(fulfillmentCenterId.Value, order.OrderItems);
					}
					else
					{
						fulfillmentCenterId = await DetermineFulfillmentCenterAsync(order.OrderItems);

						if (fulfillmentCenterId.HasValue)
						{
							order.FulfillmentCenterId = fulfillmentCenterId.Value;
							await _orderRepository.UpdateAsync(order);
							await ReserveInventoryForOrderAsync(fulfillmentCenterId.Value, order.OrderItems);
						}
						else
						{
							throw new InvalidOperationException($"Không tìm thấy trung tâm có đủ hàng để fulfill order #{orderId}. Vui lòng kiểm tra lại tồn kho.");
						}
					}
				}

				await _invoiceRepository.UpdateAmountsAsync(invoice.InvoiceId, 0m, 0m, partsAmount);

				var amount = (int)Math.Round(partsAmount);

				payment = new Domain.Entities.Payment
				{
					PaymentCode = $"PAY{paymentMethod}{DateTime.UtcNow:yyyyMMddHHmmss}{orderId}",
					InvoiceId = invoice.InvoiceId,
					PaymentMethod = paymentMethod,
					Amount = amount,
					Status = PaymentConstants.PaymentStatus.Paid,
					PaidAt = DateTime.UtcNow,
					CreatedAt = DateTime.UtcNow,
					PaidByUserID = order.Customer?.User?.UserId
				};
				payment = await _paymentRepository.CreateAsync(payment);

				try
				{
					await _promotionService.MarkUsedByOrderAsync(order.OrderId);
				}
				catch (Exception)
				{
				}

				scope.Complete();
			}
			catch
			{
				throw;
			}
		}

        if (order.Customer?.User?.UserId != null)
        {
            var uid = order.Customer.User.UserId;
            await _notificationService.SendBookingNotificationAsync(uid, $"Đơn hàng #{order.OrderId}", "Thanh toán thành công", "ORDER");
        }

		return true;
	}

	private async Task<int?> DetermineFulfillmentCenterAsync(IEnumerable<Domain.Entities.OrderItem> orderItems)
	{
		var centers = await _centerRepository.GetActiveCentersAsync();

		foreach (var center in centers)
		{
			var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(center.CenterId);
			if (inventory == null) continue;

			var inventoryParts = inventory.InventoryParts ?? new List<Domain.Entities.InventoryPart>();

			bool hasEnoughStock = orderItems.All(oi =>
			{
				var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == oi.PartId);
				if (invPart == null) return false;
				var availableQty = invPart.CurrentStock - invPart.ReservedQty;
				return availableQty >= oi.Quantity;
			});

			if (hasEnoughStock)
			{
				return center.CenterId;
			}
		}

		return null;
	}

	private async Task ReserveInventoryForOrderAsync(int centerId, IEnumerable<Domain.Entities.OrderItem> orderItems)
	{
		var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId);
		if (inventory == null)
		{
			throw new InvalidOperationException($"Không tìm thấy inventory cho center {centerId}");
		}

		var inventoryParts = inventory.InventoryParts ?? new List<Domain.Entities.InventoryPart>();

		foreach (var orderItem in orderItems)
		{
			var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == orderItem.PartId);
			if (invPart == null)
			{
				throw new InvalidOperationException($"Không tìm thấy part {orderItem.PartId} trong inventory của center {centerId}");
			}

			var availableQty = invPart.CurrentStock - invPart.ReservedQty;
			if (availableQty < orderItem.Quantity)
			{
				throw new InvalidOperationException(
					$"Không đủ hàng cho part {orderItem.PartId} trong center {centerId}. " +
					$"Hiện có: {availableQty} (CurrentStock: {invPart.CurrentStock}, ReservedQty: {invPart.ReservedQty}), cần: {orderItem.Quantity}");
			}

			invPart.CurrentStock -= orderItem.Quantity;
			invPart.ReservedQty += orderItem.Quantity;
		}

		await _inventoryRepository.UpdateInventoryAsync(inventory);
	}

	private async Task DeductInventoryFromCenterAsync(int centerId, IEnumerable<Domain.Entities.OrderItem> orderItems)
	{
		var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId);
		if (inventory == null)
		{
			throw new InvalidOperationException($"Không tìm thấy inventory cho center {centerId}");
		}

		var inventoryParts = inventory.InventoryParts ?? new List<Domain.Entities.InventoryPart>();

		foreach (var orderItem in orderItems)
		{
			var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == orderItem.PartId);
			if (invPart == null)
			{
				throw new InvalidOperationException($"Không tìm thấy part {orderItem.PartId} trong inventory của center {centerId}");
			}


			invPart.CurrentStock -= orderItem.Quantity;
			invPart.ReservedQty -= orderItem.Quantity;

		}

		await _inventoryRepository.UpdateInventoryAsync(inventory);
	}
}


