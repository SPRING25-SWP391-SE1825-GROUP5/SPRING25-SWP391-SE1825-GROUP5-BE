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
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Globalization;
using System.Transactions;

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
    private readonly IPromotionRepository _promotionRepository;
    private readonly ILogger<PaymentService> _logger;
    private readonly INotificationService _notificationService;
    private readonly ICustomerServiceCreditRepository _customerServiceCreditRepository;
    private readonly IPdfInvoiceService _pdfInvoiceService;

    private readonly EVServiceCenter.Application.Interfaces.IHoldStore _holdStore;

    public PaymentService(HttpClient httpClient, IOptions<PayOsOptions> options, IBookingRepository bookingRepository, IOrderRepository orderRepository, IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, ITechnicianRepository technicianRepository, IEmailService emailService, IWorkOrderPartRepository workOrderPartRepository, IMaintenanceChecklistRepository checklistRepository, IMaintenanceChecklistResultRepository checklistResultRepository, EVServiceCenter.Application.Interfaces.IHoldStore holdStore, IPromotionService promotionService, IPromotionRepository promotionRepository, ILogger<PaymentService> logger, ICustomerServiceCreditRepository customerServiceCreditRepository, IPdfInvoiceService pdfInvoiceService, INotificationService notificationService)
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
        _promotionRepository = promotionRepository;
        _logger = logger;
        _customerServiceCreditRepository = customerServiceCreditRepository;
        _pdfInvoiceService = pdfInvoiceService;
        _notificationService = notificationService;
        }

	/// <summary>
	/// Tạo payment link cho Booking
	/// HOÀN TOÀN ĐỘC LẬP với CreateOrderPaymentLinkAsync
	/// - Sử dụng bookingId làm PayOS orderCode
	/// - Logic tính toán amount riêng (service + parts - package - promotion)
	/// - Validation riêng cho Booking
	/// </summary>
	public async Task<string?> CreateBookingPaymentLinkAsync(int bookingId)
	{
		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking == null) throw new InvalidOperationException("Booking không tồn tại");
		if (booking.Status == "CANCELLED" || booking.Status == "CANCELED")
			throw new InvalidOperationException("Booking đã bị hủy");
		if (booking.Status != "COMPLETED")
			throw new InvalidOperationException("Chỉ có thể tạo payment link khi booking đã hoàn thành (COMPLETED). Trạng thái hiện tại: " + (booking.Status ?? "N/A"));

        // Tính tổng tiền theo logic: (gói hoặc dịch vụ lẻ) + parts - promotion
        var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
        decimal packageDiscountAmount = 0m;
        decimal packagePrice = 0m; // Giá mua gói (chỉ tính lần đầu)
        decimal promotionDiscountAmount = 0m;
        decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
            .Where(p => p.Status == "CONSUMED")
            .Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

        if (booking.AppliedCreditId.HasValue)
        {
            var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
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

        // Tính promotion discount từ UserPromotions đã áp dụng
        var userPromotions = await _promotionRepository.GetUserPromotionsByBookingAsync(booking.BookingId);
        if (userPromotions != null && userPromotions.Any())
        {
            promotionDiscountAmount = userPromotions
                .Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
                .Sum(up => up.DiscountAmount);
        }

        // Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói, không áp dụng cho parts
        var serviceComponent = booking.AppliedCreditId.HasValue ? packageDiscountAmount : serviceBasePrice;
        if (promotionDiscountAmount > serviceComponent)
        {
            promotionDiscountAmount = serviceComponent;
        }
        // total = packagePrice (nếu lần đầu) + serviceComponent - promotion + parts
        decimal totalAmount = packagePrice + serviceComponent - promotionDiscountAmount + partsAmount;

        var amount = (int)Math.Round(totalAmount); // VNĐ integer
        if (amount < _options.MinAmount) amount = _options.MinAmount;

		// Booking sử dụng bookingId làm orderCode cho PayOS
		// Lưu ý: Đảm bảo bookingId và orderId không conflict (thường có auto-increment riêng)
		var orderCode = booking.BookingId;
        var rawDesc = $"Booking #{booking.BookingId}";
        var description = rawDesc.Length > _options.DescriptionMaxLength ? rawDesc.Substring(0, _options.DescriptionMaxLength) : rawDesc;

        var returnUrl = ($"{_options.ReturnUrl ?? string.Empty}?bookingId={booking.BookingId}");
        var cancelUrl = ($"{_options.CancelUrl ?? string.Empty}?bookingId={booking.BookingId}");

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
    /// Lấy payment link hiện có từ PayOS cho Order
    /// HOÀN TOÀN ĐỘC LẬP với Booking payment methods
    /// - Sử dụng orderId làm PayOS orderCode (khác bookingId)
    /// - Validation riêng cho Order
    /// </summary>
    public async Task<string?> GetExistingOrderPaymentLinkAsync(int orderId)
    {
        // Validation chi tiết
        if (orderId <= 0)
        {
            throw new ArgumentException($"OrderId không hợp lệ: {orderId}. OrderId phải lớn hơn 0.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Không tìm thấy đơn hàng với ID: {orderId}. Vui lòng kiểm tra lại thông tin đơn hàng.");
        }

        // Kiểm tra trạng thái đơn hàng
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

        // Kiểm tra đơn hàng có items không
        if (order.OrderItems == null || !order.OrderItems.Any())
        {
            throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Đơn hàng không có sản phẩm nào.");
        }

        // Kiểm tra tổng tiền
        var totalAmount = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
        if (totalAmount <= 0)
        {
            throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Tổng tiền đơn hàng phải lớn hơn 0 (Hiện tại: {totalAmount:N0} VNĐ).");
        }

        // Kiểm tra xem đã có invoice thanh toán chưa
        if (order.Invoices != null && order.Invoices.Any())
        {
            var paidInvoice = order.Invoices.FirstOrDefault(i =>
                i.Status != null && (i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Paid || i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Completed));
            if (paidInvoice != null)
            {
                throw new InvalidOperationException($"Không thể lấy payment link cho đơn hàng #{orderId}. Đơn hàng đã có hóa đơn thanh toán (Invoice ID: {paidInvoice.InvoiceId}).");
            }
        }

        // Tất cả validation đã pass, lấy payment link từ PayOS
        // Order sử dụng orderId làm orderCode cho PayOS
        return await GetExistingPaymentLinkAsync(orderId);
    }

    /// <summary>
    /// Helper method: Lấy payment link từ PayOS bằng orderCode
    /// DÙNG CHUNG bởi cả Booking và Order, nhưng orderCode khác nhau nên không conflict
    /// - Booking: orderCode = bookingId
    /// - Order: orderCode = orderId
    /// Method này là thread-safe và không có side effects
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

    /// <summary>
    /// Tạo payment link cho Order
    /// HOÀN TOÀN ĐỘC LẬP với CreateBookingPaymentLinkAsync
    /// - Sử dụng orderId làm PayOS orderCode (khác bookingId)
    /// - Logic tính toán amount riêng (sum order items)
    /// - Validation chi tiết riêng cho Order
    /// - cancelUrl: URL riêng cho order cancel (nếu null, dùng cancel URL mặc định từ config)
    /// </summary>
    public async Task<string?> CreateOrderPaymentLinkAsync(int orderId, string? cancelUrl = null)
    {
        // Validation chi tiết riêng cho Order
        if (orderId <= 0)
        {
            throw new ArgumentException($"OrderId không hợp lệ: {orderId}. OrderId phải lớn hơn 0.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Không tìm thấy đơn hàng với ID: {orderId}. Vui lòng kiểm tra lại thông tin đơn hàng.");
        }

        // Kiểm tra trạng thái đơn hàng
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

        // Kiểm tra đơn hàng có items không
        if (order.OrderItems == null || !order.OrderItems.Any())
        {
            throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Đơn hàng không có sản phẩm nào.");
        }

        // Kiểm tra tổng tiền
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

        // Kiểm tra xem đã có invoice thanh toán chưa
        if (order.Invoices != null && order.Invoices.Any())
        {
            var paidInvoice = order.Invoices.FirstOrDefault(i =>
                i.Status != null && (i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Paid || i.Status.ToUpperInvariant() == PaymentConstants.InvoiceStatus.Completed));
            if (paidInvoice != null)
            {
                throw new InvalidOperationException($"Không thể tạo payment link cho đơn hàng #{orderId}. Đơn hàng đã có hóa đơn thanh toán (Invoice ID: {paidInvoice.InvoiceId}).");
            }
        }

        // Order sử dụng orderId làm orderCode cho PayOS
        // Lưu ý: Đảm bảo bookingId và orderId không conflict (thường có auto-increment riêng)
        var orderCode = orderId;
        var rawDesc = $"Order #{order.OrderId}";
        var description = rawDesc.Length > _options.DescriptionMaxLength ? rawDesc.Substring(0, _options.DescriptionMaxLength) : rawDesc;

        var returnUrl = (_options.ReturnUrl ?? string.Empty);
        // Sử dụng cancelUrl riêng cho order nếu được truyền, nếu không thì dùng cancel URL mặc định từ config
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

        // Xử lý trường hợp "Đơn thanh toán đã tồn tại" (code 231)
        if (!response.IsSuccessStatusCode)
        {
            var errorJson = JsonDocument.Parse(responseText).RootElement;
            var code = errorJson.TryGetProperty("code", out var codeElem) ? codeElem.GetString() : null;
            var desc = errorJson.TryGetProperty("desc", out var errorDescElem) ? errorDescElem.GetString() : null;

            if (code == PaymentConstants.PayOSErrorCodes.PaymentExists && desc?.Contains("Đơn thanh toán đã tồn tại") == true)
            {
                _logger.LogInformation("PayOS trả về code 231 (payment exists) cho orderCode {OrderCode}, đang lấy payment link hiện có...", orderCode);

                // Thử lấy checkoutUrl từ error response data trước (nếu có)
                if (errorJson.TryGetProperty("data", out var errorDataElem) && errorDataElem.ValueKind == JsonValueKind.Object)
                {
                    if (errorDataElem.TryGetProperty("checkoutUrl", out var errorUrlElem) && errorUrlElem.ValueKind == JsonValueKind.String)
                    {
                        var errorCheckoutUrl = errorUrlElem.GetString();
                        if (!string.IsNullOrEmpty(errorCheckoutUrl))
                        {
                            _logger.LogInformation("Tìm thấy checkoutUrl trong error response cho orderCode {OrderCode}", orderCode);
                            return errorCheckoutUrl;
                        }
                    }
                }

                // Nếu không có trong error response, thử lấy từ PayOS API
                var existingUrl = await GetExistingPaymentLinkAsync(orderCode);
                if (!string.IsNullOrEmpty(existingUrl))
                {
                    _logger.LogInformation("Đã lấy được payment link hiện có từ PayOS cho orderCode {OrderCode}", orderCode);
                    return existingUrl;
                }

                // Nếu không lấy được link cũ, throw exception với message chi tiết
                _logger.LogWarning("Không thể lấy payment link hiện có từ PayOS cho orderCode {OrderCode}. PayOS đã báo payment exists nhưng không tìm thấy link.", orderCode);
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
			_logger.LogWarning("ConfirmPaymentAsync called with invalid bookingId: {BookingId}", bookingId);
			return false;
		}

		_logger.LogInformation("=== BẮT ĐẦU CONFIRM PAYMENT ===");
		_logger.LogInformation("ConfirmPaymentAsync called with bookingId: {BookingId}, paymentMethod: {PaymentMethod}", bookingId, paymentMethod);

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

			// Khai báo variables bên ngoài scope để sử dụng sau
			Domain.Entities.Invoice? invoice = null;
			Domain.Entities.Payment? payment = null;

			// Sử dụng TransactionScope để đảm bảo tất cả thao tác database được commit cùng lúc
			using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				try
				{
					// Cập nhật trạng thái booking
					_logger.LogInformation("Cập nhật booking {BookingId} từ {OldStatus} thành PAID", booking.BookingId, booking.Status);
					booking.Status = "PAID";
					booking.UpdatedAt = DateTime.UtcNow;
					await _bookingRepository.UpdateBookingAsync(booking);
					_logger.LogInformation("Đã cập nhật booking {BookingId} thành PAID", booking.BookingId);

					// Tạo hoặc cập nhật invoice
					_logger.LogInformation("Tạo/cập nhật invoice cho booking {BookingId}", booking.BookingId);
					invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
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
					decimal packagePrice = 0m; // Giá mua gói (chỉ tính lần đầu)
					decimal promotionDiscountAmount = 0m;
					decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
						.Where(p => p.Status == "CONSUMED")
						.Sum(p => p.QuantityUsed * (p.Part?.Price ?? 0));

					if (booking.AppliedCreditId.HasValue)
					{
						var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
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
					var userPromotions = await _promotionRepository.GetUserPromotionsByBookingAsync(booking.BookingId);
					if (userPromotions != null && userPromotions.Any())
					{
						promotionDiscountAmount = userPromotions
							.Where(up => string.Equals(up.Status, "APPLIED", StringComparison.OrdinalIgnoreCase))
							.Sum(up => up.DiscountAmount);
					}

                    // Khuyến mãi chỉ áp dụng cho phần dịch vụ/gói, không áp dụng cho parts
                    var serviceComponent = booking.AppliedCreditId.HasValue ? packageDiscountAmount : serviceBasePrice;
                    if (promotionDiscountAmount > serviceComponent)
                    {
                        promotionDiscountAmount = serviceComponent;
                    }
                    decimal paymentAmount = packagePrice + serviceComponent - promotionDiscountAmount + partsAmount;

					// Tạo payment record
					_logger.LogInformation("Tạo payment record cho booking {BookingId} với amount {Amount}, method {PaymentMethod}", booking.BookingId, paymentAmount, paymentMethod);
					payment = new Domain.Entities.Payment
					{
						InvoiceId = invoice.InvoiceId,
						Amount = (int)Math.Round(paymentAmount),
						PaymentMethod = paymentMethod, // Sử dụng paymentMethod từ parameter (SEPAY hoặc PAYOS)
						Status = "COMPLETED",
						PaymentCode = bookingId.ToString(),
						CreatedAt = DateTime.UtcNow,
						PaidAt = DateTime.UtcNow,
						PaidByUserID = booking.Customer?.User?.UserId
					};
					await _paymentRepository.CreateAsync(payment);
					_logger.LogInformation("Đã tạo payment {PaymentId} cho booking {BookingId} với method {PaymentMethod}", payment.PaymentId, booking.BookingId, paymentMethod);

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

					// Commit transaction - tất cả thao tác database sẽ được lưu cùng lúc
					scope.Complete();
					_logger.LogInformation("=== TRANSACTION COMMITTED SUCCESSFULLY ===");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "=== TRANSACTION ROLLBACK - Lỗi cập nhật database ===");
					throw; // Re-throw để caller biết có lỗi
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
        if (booking.Customer?.User?.UserId != null)
        {
            var uid = booking.Customer.User.UserId;
            await _notificationService.SendBookingNotificationAsync(uid, $"Đặt lịch #{booking.BookingId}", "Thanh toán thành công", "BOOKING");
        }
		return true;
	}
		else
		{
			_logger.LogInformation("Thanh toán chưa thành công hoặc không tìm thấy booking. Booking: {BookingFound}", booking != null ? "Found" : "Not Found");
		return false;
		}
	}

	public async Task<bool> ConfirmOrderPaymentAsync(int orderId, string paymentMethod = "PAYOS")
	{
		if (orderId <= 0)
		{
			_logger.LogWarning("ConfirmOrderPaymentAsync called with invalid orderId: {OrderId}", orderId);
			return false;
		}

		_logger.LogInformation("=== BẮT ĐẦU CONFIRM ORDER PAYMENT ===");
		_logger.LogInformation("ConfirmOrderPaymentAsync called with orderId: {OrderId}, paymentMethod: {PaymentMethod}", orderId, paymentMethod);

		var order = await _orderRepository.GetByIdAsync(orderId);
		if (order == null)
		{
			_logger.LogWarning("Không tìm thấy order với ID: {OrderId}", orderId);
			return false;
		}

		_logger.LogInformation("Tìm thấy order {OrderId} với status hiện tại: {CurrentStatus}", orderId, order.Status);

		if (order.Status == "PAID" || order.Status == "COMPLETED")
		{
			_logger.LogInformation("Order {OrderId} đã được thanh toán rồi", orderId);
			return true;
		}

		_logger.LogInformation("=== BẮT ĐẦU CẬP NHẬT DATABASE ===");
		_logger.LogInformation("Thanh toán thành công cho order {OrderId}, đang cập nhật database...", order.OrderId);

		Domain.Entities.Invoice? invoice = null;
		Domain.Entities.Payment? payment = null;

		using (var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
		{
			try
			{
				_logger.LogInformation("Cập nhật order {OrderId} từ {OldStatus} thành PAID", order.OrderId, order.Status);
				order.Status = "PAID";
				order.UpdatedAt = DateTime.UtcNow;
				await _orderRepository.UpdateAsync(order);
				_logger.LogInformation("Đã cập nhật order {OrderId} thành PAID", order.OrderId);

				_logger.LogInformation("Tạo/cập nhật invoice cho order {OrderId}", order.OrderId);
				var existingInvoices = order.Invoices?.ToList() ?? new List<Domain.Entities.Invoice>();
				invoice = existingInvoices.FirstOrDefault();

				if (invoice == null)
				{
					_logger.LogInformation("Tạo invoice mới cho order {OrderId}", order.OrderId);
					invoice = new Domain.Entities.Invoice
					{
						OrderId = order.OrderId,
						CustomerId = order.CustomerId,
						Email = order.Customer?.User?.Email,
						Phone = order.Customer?.User?.PhoneNumber,
						Status = "PAID",
						CreatedAt = DateTime.UtcNow
					};
					invoice = await _invoiceRepository.CreateMinimalAsync(invoice);
					_logger.LogInformation("Đã tạo invoice {InvoiceId} cho order {OrderId}", invoice.InvoiceId, order.OrderId);
				}
				else
				{
					_logger.LogInformation("Invoice {InvoiceId} đã tồn tại cho order {OrderId}", invoice.InvoiceId, order.OrderId);
					invoice.Status = "PAID";
				}

				var totalAmount = order.OrderItems?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
				var amount = (int)Math.Round(totalAmount);

				_logger.LogInformation("Tạo payment record cho order {OrderId}", order.OrderId);
				payment = new Domain.Entities.Payment
				{
					PaymentCode = $"PAY{paymentMethod}{DateTime.UtcNow:yyyyMMddHHmmss}{orderId}",
					InvoiceId = invoice.InvoiceId,
					PaymentMethod = paymentMethod,
					Amount = amount,
					Status = "PAID",
					PaidAt = DateTime.UtcNow,
					CreatedAt = DateTime.UtcNow,
					PaidByUserID = order.Customer?.User?.UserId
				};
				payment = await _paymentRepository.CreateAsync(payment);
				_logger.LogInformation("Đã tạo payment {PaymentId} cho order {OrderId}", payment.PaymentId, order.OrderId);

				scope.Complete();
				_logger.LogInformation("=== CẬP NHẬT DATABASE THÀNH CÔNG ===");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "=== LỖI KHI CẬP NHẬT DATABASE ===");
				throw;
			}
		}

		_logger.LogInformation("Payment confirmed for order {OrderId}, invoice {InvoiceId}, payment {PaymentId}", order.OrderId, invoice?.InvoiceId, payment?.PaymentId);
        if (order.Customer?.User?.UserId != null)
        {
            var uid = order.Customer.User.UserId;
            await _notificationService.SendBookingNotificationAsync(uid, $"Đơn hàng #{order.OrderId}", "Thanh toán thành công", "ORDER");
        }
		return true;
	}
}


