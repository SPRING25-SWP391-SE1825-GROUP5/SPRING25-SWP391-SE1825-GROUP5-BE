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
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICenterRepository _centerRepository;

    private readonly EVServiceCenter.Application.Interfaces.IHoldStore _holdStore;

    public PaymentService(HttpClient httpClient, IOptions<PayOsOptions> options, IBookingRepository bookingRepository, IOrderRepository orderRepository, IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, ITechnicianRepository technicianRepository, IEmailService emailService, IWorkOrderPartRepository workOrderPartRepository, IMaintenanceChecklistRepository checklistRepository, IMaintenanceChecklistResultRepository checklistResultRepository, EVServiceCenter.Application.Interfaces.IHoldStore holdStore, IPromotionService promotionService, IPromotionRepository promotionRepository, ILogger<PaymentService> logger, ICustomerServiceCreditRepository customerServiceCreditRepository, IPdfInvoiceService pdfInvoiceService, INotificationService notificationService, IInventoryRepository inventoryRepository, ICenterRepository centerRepository)
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
        _inventoryRepository = inventoryRepository;
        _centerRepository = centerRepository;
        }

	/// <summary>
	/// Tạo payment link cho Booking
	/// HOÀN TOÀN ĐỘC LẬP với CreateOrderPaymentLinkAsync
	/// - Sử dụng bookingId làm PayOS orderCode (1, 2, 3, ...)
	/// - Order sẽ dùng orderCode = orderId + 1000000 để tránh conflict
	/// - Logic tính toán amount riêng (service + parts - package - promotion)
	/// - Validation riêng cho Booking
	/// </summary>
	public async Task<string?> CreateBookingPaymentLinkAsync(int bookingId, bool isRetry = false)
	{
		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking == null) throw new InvalidOperationException("Booking không tồn tại");
		if (booking.Status == BookingStatusConstants.Cancelled || booking.Status == BookingStatusConstants.Canceled)
			throw new InvalidOperationException("Booking đã bị hủy");
		if (booking.Status != BookingStatusConstants.Completed)
			throw new InvalidOperationException($"Chỉ có thể tạo payment link khi booking đã hoàn thành ({BookingStatusConstants.Completed}). Trạng thái hiện tại: " + (booking.Status ?? "N/A"));

        // Tính tổng tiền theo logic: (gói hoặc dịch vụ lẻ) + parts - promotion
        var serviceBasePrice = booking.Service?.BasePrice ?? 0m;
        decimal packageDiscountAmount = 0m;
        decimal packagePrice = 0m; // Giá mua gói (chỉ tính lần đầu)
        decimal promotionDiscountAmount = 0m;
        decimal partsAmount = (await _workOrderPartRepository.GetByBookingIdAsync(booking.BookingId))
            .Where(p => p.Status == "CONSUMED") // TODO: Create WorkOrderPartStatusConstants
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

		// Generate hoặc lấy PayOSOrderCode từ Booking
		// Nếu chưa có, generate unique PayOSOrderCode và lưu vào database
		int orderCode;
		if (booking.PayOSOrderCode.HasValue)
		{
			orderCode = booking.PayOSOrderCode.Value;
			_logger.LogInformation("Sử dụng PayOSOrderCode hiện có cho bookingId {BookingId}: {OrderCode}", bookingId, orderCode);
		}
		else
		{
			orderCode = await GenerateUniquePayOSOrderCodeAsync();
			// Update trực tiếp PayOSOrderCode trong database
			await _bookingRepository.UpdatePayOSOrderCodeAsync(bookingId, orderCode);
			booking.PayOSOrderCode = orderCode; // Update local entity để dùng tiếp
			_logger.LogInformation("Generated và lưu PayOSOrderCode mới cho bookingId {BookingId}: {OrderCode}", bookingId, orderCode);
		}
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

        // Parse response để check code (PayOS có thể trả về HTTP 200 nhưng code 231 trong body)
        var json = JsonDocument.Parse(responseText).RootElement;
        var code = json.TryGetProperty("code", out var codeElem) ? codeElem.GetString() : null;
        var desc = json.TryGetProperty("desc", out var descElem) ? descElem.GetString() : null;

        // Xử lý trường hợp "Đơn thanh toán đã tồn tại" (code 231)
        // PayOS có thể trả về HTTP 200 nhưng code 231 trong response body
        if (code == "231" && desc?.Contains("Đơn thanh toán đã tồn tại") == true)
        {
            _logger.LogInformation("PayOS trả về code 231 (payment exists) cho bookingId {BookingId}, đang lấy payment link hiện có...", bookingId);

            // Ưu tiên lấy checkoutUrl trực tiếp từ response nếu có
            if (json.TryGetProperty("data", out var errData) && errData.ValueKind == JsonValueKind.Object)
            {
                if (errData.TryGetProperty("checkoutUrl", out var errUrl) && errUrl.ValueKind == JsonValueKind.String)
                {
                    var reuseUrl = errUrl.GetString();
                    if (!string.IsNullOrEmpty(reuseUrl))
                    {
                        _logger.LogInformation("Tìm thấy checkoutUrl trong response cho bookingId {BookingId}", bookingId);
                        return reuseUrl;
                    }
                }
            }

            // Fallback: Lấy link cũ từ PayOS API
            var existingUrl = await GetExistingPaymentLinkAsync(orderCode);
            if (!string.IsNullOrEmpty(existingUrl))
            {
                _logger.LogInformation("Đã lấy được payment link hiện có từ PayOS cho bookingId {BookingId}", bookingId);
                return existingUrl;
            }

            // Nếu không lấy được link cũ (có thể payment đã CANCELLED), thử cancel và tạo lại
            // Chỉ retry 1 lần để tránh infinite loop
            if (!isRetry)
            {
                _logger.LogInformation("Không tìm thấy checkoutUrl cho bookingId {BookingId}, thử cancel payment cũ và tạo lại...", bookingId);
                var cancelled = await CancelPaymentLinkAsync(orderCode);
                if (cancelled)
                {
                    _logger.LogInformation("Đã cancel payment cũ cho bookingId {BookingId}, retry tạo payment link...", bookingId);
                    // Retry tạo payment link sau khi cancel (chỉ retry 1 lần)
                    return await CreateBookingPaymentLinkAsync(bookingId, isRetry: true);
                }
            }

            // Nếu không cancel được hoặc đã retry rồi, throw exception với message rõ ràng
            _logger.LogWarning("Không thể lấy payment link hiện có từ PayOS cho bookingId {BookingId}. PayOS đã báo payment exists nhưng không tìm thấy link và không thể cancel.", bookingId);
            throw new InvalidOperationException($"Đơn thanh toán đã tồn tại trên PayOS cho booking #{bookingId}, nhưng không thể lấy được payment link. Vui lòng liên hệ hỗ trợ hoặc thử lại sau.");
        }

        // Nếu không phải lỗi 231, check HTTP status code
        if (!response.IsSuccessStatusCode)
        {
            var message =
                desc
                ?? (json.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String ? msgElem.GetString() : null)
                ?? "Không nhận được checkoutUrl từ PayOS";
            throw new InvalidOperationException($"Tạo link PayOS thất bại: {message}. Response: {responseText}");
        }

        // Parse success response
        if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object &&
            dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
        {
            return urlElem.GetString();
        }

        // Nếu không tìm thấy checkoutUrl trong success response
        var errorMessage =
            desc
            ?? (json.TryGetProperty("message", out var msgElem2) && msgElem2.ValueKind == JsonValueKind.String ? msgElem2.GetString() : null)
            ?? "Không nhận được checkoutUrl từ PayOS";
        throw new InvalidOperationException($"Tạo link PayOS thất bại: {errorMessage}. Response: {responseText}");
    }

    /// <summary>
    /// Lấy payment link hiện có từ PayOS cho Order
    /// HOÀN TOÀN ĐỘC LẬP với Booking payment methods
    /// - Sử dụng orderId + 1000000 làm PayOS orderCode để tránh conflict với Booking
    /// - Booking: orderCode = bookingId (1, 2, 3, ...)
    /// - Order: orderCode = orderId + 1000000 (1000001, 1000002, ...)
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
        // Sử dụng PayOSOrderCode đã lưu trong Order
        if (!order.PayOSOrderCode.HasValue)
        {
            throw new InvalidOperationException($"Order #{orderId} chưa có PayOSOrderCode. Vui lòng tạo payment link trước.");
        }
        return await GetExistingPaymentLinkAsync(order.PayOSOrderCode.Value);
    }

    /// <summary>
    /// Helper method: Lấy payment link từ PayOS bằng orderCode
    /// DÙNG CHUNG bởi cả Booking và Order, nhưng orderCode khác nhau nên không conflict
    /// - Booking: orderCode = bookingId (1, 2, 3, ...)
    /// - Order: orderCode = orderId + 1000000 (1000001, 1000002, 1000003, ...)
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

            // Check status của payment
            string? paymentStatus = null;
            if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object)
            {
                if (dataElem.TryGetProperty("status", out var statusElem) && statusElem.ValueKind == JsonValueKind.String)
                {
                    paymentStatus = statusElem.GetString();
                }

                // Nếu có checkoutUrl, trả về
                if (dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
                {
                    var checkoutUrl = urlElem.GetString();
                    if (!string.IsNullOrEmpty(checkoutUrl))
                    {
                        return checkoutUrl;
                    }
                }
            }

            // Nếu không có checkoutUrl, check status
            if (paymentStatus == PaymentConstants.PaymentStatus.Cancelled)
            {
                _logger.LogInformation("Payment cho orderCode {OrderCode} đã bị CANCELLED, sẽ cancel và tạo lại", orderCode);
                return null; // Return null để trigger cancel và tạo lại
            }

            _logger.LogWarning("Không tìm thấy checkoutUrl trong response cho orderCode {OrderCode}, status: {Status}, Response: {ResponseText}", orderCode, paymentStatus, responseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy payment link cũ cho orderCode {OrderCode}", orderCode);
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
                _logger.LogInformation("Đã cancel payment link cho orderCode {OrderCode}", orderCode);
                return true;
            }

            // Nếu PayOS trả về 404, có thể payment đã không còn tồn tại (đã bị xóa hoặc đã CANCELLED)
            // Trong trường hợp này, coi như đã cancel thành công
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Payment cho orderCode {OrderCode} không còn tồn tại trên PayOS (404), coi như đã cancel thành công", orderCode);
                return true;
            }

            _logger.LogWarning("Không thể cancel payment link cho orderCode {OrderCode}: {ResponseText}", orderCode, responseText);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cancel payment link cho orderCode {OrderCode}", orderCode);
            return false;
        }
    }

    /// <summary>
    /// Tạo payment link cho Order
    /// HOÀN TOÀN ĐỘC LẬP với CreateBookingPaymentLinkAsync
    /// - Sử dụng orderId + 1000000 làm PayOS orderCode để tránh conflict với Booking
    /// - Booking: orderCode = bookingId (1, 2, 3, ...)
    /// - Order: orderCode = orderId + 1000000 (1000001, 1000002, ...)
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

        // Generate hoặc lấy PayOSOrderCode từ Order
        // Nếu chưa có, generate unique PayOSOrderCode và lưu vào database
        int orderCode;
        if (order.PayOSOrderCode.HasValue)
        {
            orderCode = order.PayOSOrderCode.Value;
            _logger.LogInformation("Sử dụng PayOSOrderCode hiện có cho orderId {OrderId}: {OrderCode}", orderId, orderCode);
        }
        else
        {
            orderCode = await GenerateUniquePayOSOrderCodeAsync();
            // Update trực tiếp PayOSOrderCode trong database
            await _orderRepository.UpdatePayOSOrderCodeAsync(orderId, orderCode);
            order.PayOSOrderCode = orderCode; // Update local entity để dùng tiếp
            _logger.LogInformation("Generated và lưu PayOSOrderCode mới cho orderId {OrderId}: {OrderCode}", orderId, orderCode);
        }
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

    /// <summary>
    /// Generate unique PayOS orderCode
    /// Sử dụng timestamp + random để đảm bảo unique
    /// Format: YYYYMMDDHHMMSS + random 4 digits (1000-9999)
    /// Ví dụ: 20251107120000 + 1234 = 202511071200001234
    /// </summary>
    private int GenerateUniquePayOSOrderCode()
    {
        var random = new Random();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var randomPart = random.Next(1000, 10000); // 4 digits: 1000-9999
        var orderCodeStr = timestamp + randomPart.ToString();

        // Parse to int, nếu quá lớn thì dùng hash
        if (int.TryParse(orderCodeStr, out var orderCode) && orderCode > 0)
        {
            return orderCode;
        }

        // Fallback: dùng hash của timestamp + random
        var hash = Math.Abs((timestamp + randomPart).GetHashCode());
        // Đảm bảo là số dương và đủ lớn để tránh conflict
        return Math.Max(100000000, hash);
    }

    /// <summary>
    /// Generate và đảm bảo unique PayOSOrderCode
    /// Check cả Booking và Order để đảm bảo không trùng
    /// Retry nếu trùng (rất hiếm)
    /// </summary>
    private async Task<int> GenerateUniquePayOSOrderCodeAsync(int maxRetries = 10)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var orderCode = GenerateUniquePayOSOrderCode();

            // Check xem có Booking hoặc Order nào đã dùng PayOSOrderCode này chưa
            var bookingExists = await _bookingRepository.PayOSOrderCodeExistsAsync(orderCode);
            var orderExists = await _orderRepository.PayOSOrderCodeExistsAsync(orderCode);

            if (!bookingExists && !orderExists)
            {
                _logger.LogInformation("Generated unique PayOSOrderCode: {OrderCode}", orderCode);
                return orderCode;
            }

            _logger.LogWarning("Generated PayOSOrderCode {OrderCode} đã tồn tại, retry {Retry}/{MaxRetries}", orderCode, i + 1, maxRetries);
            await Task.Delay(10); // Đợi một chút để timestamp thay đổi
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
					booking.Status = BookingStatusConstants.Paid;
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
							Status = PaymentConstants.PaymentStatus.Paid,
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
						invoice.Status = PaymentConstants.InvoiceStatus.Paid;
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
						Status = BookingStatusConstants.Completed,
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

		if (order.Status == PaymentConstants.OrderStatus.Paid || order.Status == PaymentConstants.OrderStatus.Completed)
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
				order.Status = PaymentConstants.OrderStatus.Paid;
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
						Status = PaymentConstants.InvoiceStatus.Paid,
						CreatedAt = DateTime.UtcNow
					};
					invoice = await _invoiceRepository.CreateMinimalAsync(invoice);
					_logger.LogInformation("Đã tạo invoice {InvoiceId} cho order {OrderId}", invoice.InvoiceId, order.OrderId);
				}
				else
				{
					_logger.LogInformation("Invoice {InvoiceId} đã tồn tại cho order {OrderId}", invoice.InvoiceId, order.OrderId);
					invoice.Status = PaymentConstants.InvoiceStatus.Paid;
				}

				// Tính PartsAmount từ OrderItems
				var partsAmount = order.OrderItems?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
				_logger.LogInformation("Tính PartsAmount cho order {OrderId}: {PartsAmount:N0} VNĐ", order.OrderId, partsAmount);

				// Xác định fulfillment center
				// Ưu tiên: Sử dụng FulfillmentCenterId đã lưu trong Order (được chọn từ FE)
				// Fallback: Tự động chọn center có đủ stock (logic cũ)
				int? fulfillmentCenterId = null;
				if (order.OrderItems != null && order.OrderItems.Any())
				{
					// Nếu Order đã có FulfillmentCenterId (được chọn từ FE khi tạo order)
					if (order.FulfillmentCenterId.HasValue)
					{
						fulfillmentCenterId = order.FulfillmentCenterId.Value;
						_logger.LogInformation("Sử dụng FulfillmentCenterId đã lưu {CenterId} cho order {OrderId} (được chọn từ FE)", 
							fulfillmentCenterId.Value, order.OrderId);

						// Validate lại stock trước khi trừ (có thể đã hết hàng từ lúc tạo order đến lúc thanh toán)
						var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(fulfillmentCenterId.Value);
						if (inventory == null)
						{
							_logger.LogWarning("Inventory không tồn tại cho center {CenterId} của order {OrderId}", 
								fulfillmentCenterId.Value, order.OrderId);
							throw new InvalidOperationException($"Kho của chi nhánh ID {fulfillmentCenterId.Value} không tồn tại. Vui lòng chọn chi nhánh khác.");
						}

						var inventoryParts = inventory.InventoryParts ?? new List<Domain.Entities.InventoryPart>();
						foreach (var orderItem in order.OrderItems)
						{
							var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == orderItem.PartId);
							if (invPart == null)
							{
								_logger.LogWarning("Part {PartId} không có trong inventory của center {CenterId} cho order {OrderId}", 
									orderItem.PartId, fulfillmentCenterId.Value, order.OrderId);
								throw new InvalidOperationException(
									$"Phụ tùng ID {orderItem.PartId} không có trong kho của chi nhánh ID {fulfillmentCenterId.Value}. " +
									$"Vui lòng chọn chi nhánh khác.");
							}

							if (invPart.CurrentStock < orderItem.Quantity)
							{
								_logger.LogWarning("Không đủ stock cho part {PartId} trong center {CenterId} cho order {OrderId}. Hiện có: {CurrentStock}, cần: {Quantity}", 
									orderItem.PartId, fulfillmentCenterId.Value, order.OrderId, invPart.CurrentStock, orderItem.Quantity);
								throw new InvalidOperationException(
									$"Không đủ hàng cho phụ tùng ID {orderItem.PartId} tại chi nhánh ID {fulfillmentCenterId.Value}. " +
									$"Hiện có: {invPart.CurrentStock}, cần: {orderItem.Quantity}. Vui lòng chọn chi nhánh khác.");
							}
						}

						// Trừ kho từ fulfillment center đã chọn
						_logger.LogInformation("Đang trừ kho từ fulfillment center {CenterId} (được chọn từ FE)...", fulfillmentCenterId.Value);
						await DeductInventoryFromCenterAsync(fulfillmentCenterId.Value, order.OrderItems);
						_logger.LogInformation("Đã trừ kho thành công từ fulfillment center {CenterId}", fulfillmentCenterId.Value);
					}
					else
					{
						// Fallback: Tự động chọn center có đủ stock (logic cũ - backward compatibility)
						_logger.LogInformation("Order {OrderId} chưa có FulfillmentCenterId, đang tự động chọn center có đủ stock...", order.OrderId);
						fulfillmentCenterId = await DetermineFulfillmentCenterAsync(order.OrderItems);

						if (fulfillmentCenterId.HasValue)
						{
							_logger.LogInformation("Đã tự động chọn fulfillment center {CenterId} cho order {OrderId}", 
								fulfillmentCenterId.Value, order.OrderId);

							// Lưu fulfillment center vào Order
							order.FulfillmentCenterId = fulfillmentCenterId.Value;
							await _orderRepository.UpdateAsync(order);
							_logger.LogInformation("Đã lưu fulfillment center {CenterId} vào Order {OrderId}", 
								fulfillmentCenterId.Value, order.OrderId);

							// Trừ kho từ fulfillment center
							_logger.LogInformation("Đang trừ kho từ fulfillment center {CenterId}...", fulfillmentCenterId.Value);
							await DeductInventoryFromCenterAsync(fulfillmentCenterId.Value, order.OrderItems);
							_logger.LogInformation("Đã trừ kho thành công từ fulfillment center {CenterId}", fulfillmentCenterId.Value);
						}
						else
						{
							_logger.LogWarning("Không tìm thấy fulfillment center có đủ stock cho order {OrderId}", order.OrderId);
							throw new InvalidOperationException($"Không tìm thấy trung tâm có đủ hàng để fulfill order #{orderId}. Vui lòng kiểm tra lại tồn kho.");
						}
					}
				}
				else
				{
					_logger.LogWarning("Order {OrderId} không có OrderItems", order.OrderId);
				}

				// Update Invoice.PartsAmount
				_logger.LogInformation("Cập nhật PartsAmount vào Invoice {InvoiceId}: {PartsAmount:N0} VNĐ", invoice.InvoiceId, partsAmount);
				await _invoiceRepository.UpdateAmountsAsync(invoice.InvoiceId, 0m, 0m, partsAmount);

				var totalAmount = partsAmount;
				var amount = (int)Math.Round(totalAmount);

				_logger.LogInformation("Tạo payment record cho order {OrderId}", order.OrderId);
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

	/// <summary>
	/// Xác định fulfillment center có đủ stock cho tất cả OrderItems
	/// Trả về center đầu tiên có đủ stock, hoặc null nếu không tìm thấy
	/// </summary>
	private async Task<int?> DetermineFulfillmentCenterAsync(IEnumerable<Domain.Entities.OrderItem> orderItems)
	{
		var centers = await _centerRepository.GetActiveCentersAsync();

		foreach (var center in centers)
		{
			var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(center.CenterId);
			if (inventory == null) continue;

			var inventoryParts = inventory.InventoryParts ?? new List<Domain.Entities.InventoryPart>();

			// Kiểm tra xem center này có đủ stock cho tất cả items không
			bool hasEnoughStock = orderItems.All(oi =>
			{
				var invPart = inventoryParts.FirstOrDefault(ip => ip.PartId == oi.PartId);
				return invPart != null && invPart.CurrentStock >= oi.Quantity;
			});

			if (hasEnoughStock)
			{
				_logger.LogInformation("Tìm thấy fulfillment center {CenterId} có đủ stock", center.CenterId);
				return center.CenterId;
			}
		}

		_logger.LogWarning("Không tìm thấy fulfillment center có đủ stock");
		return null;
	}

	/// <summary>
	/// Trừ kho từ fulfillment center cho tất cả OrderItems
	/// </summary>
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

			if (invPart.CurrentStock < orderItem.Quantity)
			{
				throw new InvalidOperationException($"Không đủ stock cho part {orderItem.PartId} trong center {centerId}. Hiện có: {invPart.CurrentStock}, cần: {orderItem.Quantity}");
			}

			// Trừ kho
			invPart.CurrentStock -= orderItem.Quantity;
			_logger.LogInformation("Đã trừ {Quantity} units của part {PartId} từ center {CenterId}. Stock còn lại: {RemainingStock}",
				orderItem.Quantity, orderItem.PartId, centerId, invPart.CurrentStock);
		}

		// Save changes
		await _inventoryRepository.UpdateInventoryAsync(inventory);
		_logger.LogInformation("Đã cập nhật inventory cho center {CenterId}", centerId);
	}
}


