using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Globalization;

namespace EVServiceCenter.Application.Service;

public class PaymentService
{
	private readonly HttpClient _httpClient;
	private readonly PayOsOptions _options;
	private readonly IBookingRepository _bookingRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
	private readonly IInvoiceRepository _invoiceRepository;
	private readonly IPaymentRepository _paymentRepository;
    private readonly ITechnicianRepository _technicianRepository;

    public PaymentService(HttpClient httpClient, IOptions<PayOsOptions> options, IBookingRepository bookingRepository, IWorkOrderRepository workOrderRepository, IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, ITechnicianRepository technicianRepository)
	{
		_httpClient = httpClient;
		_options = options.Value;
		_bookingRepository = bookingRepository;
		_workOrderRepository = workOrderRepository;
		_invoiceRepository = invoiceRepository;
		_paymentRepository = paymentRepository;
        _technicianRepository = technicianRepository;
	}

	public async Task<string> CreateBookingPaymentLinkAsync(int bookingId)
	{
		var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
		if (booking == null) throw new InvalidOperationException("Booking không tồn tại");
		if (booking.Status == "CANCELLED") throw new InvalidOperationException("Booking đã bị hủy");

		var amount = (int)Math.Round((booking.TotalEstimatedCost ?? 0m)); // VNĐ integer
		if (amount < 1000) amount = 1000; // Tối thiểu theo PayOS

		var orderCode = booking.BookingId; // PayOS yêu cầu là số
		var rawDesc = $"Booking {booking.BookingCode}";
		var description = rawDesc.Length > 25 ? rawDesc.Substring(0, 25) : rawDesc;

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
				new { name = $"Booking {booking.BookingCode}", quantity = 1, price = amount }
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

	public async Task<bool> ConfirmPaymentAsync(string orderCode)
	{
		if (string.IsNullOrWhiteSpace(orderCode)) return false;

		var getUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests/{orderCode}";
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(getUrl));
		request.Headers.Add("x-client-id", _options.ClientId);
		request.Headers.Add("x-api-key", _options.ApiKey);

		var response = await _httpClient.SendAsync(request);
		response.EnsureSuccessStatusCode();
		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		var status = json.GetProperty("data").GetProperty("status").GetString();

		Domain.Entities.Booking booking = null;
		if (int.TryParse(orderCode, out var bookingIdFromOrder))
		{
			booking = await _bookingRepository.GetBookingByIdAsync(bookingIdFromOrder);
		}
		if (booking == null)
		{
			booking = await _bookingRepository.GetBookingByCodeAsync(orderCode);
		}
		if (booking == null) return false;

		if (status == "PAID" || status == "SUCCESS" || status == "COMPLETED")
		{
			booking.Status = "CONFIRMED";

			// 1) Ensure WorkOrder exists
            var workOrder = await _workOrderRepository.GetByBookingIdAsync(booking.BookingId);
			if (workOrder == null)
			{
                // Chọn technician bất kỳ thuộc center của booking, nếu không có thì lấy bất kỳ kỹ thuật viên nào
                var techId = 0;
                var techsInCenter = await _technicianRepository.GetTechniciansByCenterIdAsync(booking.CenterId);
                if (techsInCenter != null && techsInCenter.Count > 0) techId = techsInCenter[0].TechnicianId;
                else
                {
                    var allTechs = await _technicianRepository.GetAllTechniciansAsync();
                    if (allTechs != null && allTechs.Count > 0) techId = allTechs[0].TechnicianId;
                }
                if (techId == 0) throw new InvalidOperationException("Không có kỹ thuật viên để lập WorkOrder");
				workOrder = new Domain.Entities.WorkOrder
				{
					WorkOrderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{booking.BookingId}",
					BookingId = booking.BookingId,
                    TechnicianId = techId,
					Status = "COMPLETED",
					StartTime = DateTime.UtcNow,
					EndTime = DateTime.UtcNow,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				workOrder = await _workOrderRepository.CreateAsync(workOrder);
			}

			// 2) Ensure Invoice exists
			var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
			if (invoice == null)
			{
				invoice = new Domain.Entities.Invoice
				{
					InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{workOrder.WorkOrderId}",
					WorkOrderId = workOrder.WorkOrderId,
					CustomerId = booking.CustomerId,
					BillingName = booking.Customer?.User?.FullName ?? "Guest",
					BillingPhone = booking.Customer?.User?.PhoneNumber,
					BillingAddress = booking.Center?.Address,
					Status = "PAID",
					TotalAmount = booking.TotalEstimatedCost ?? 0m,
					CreatedAt = DateTime.UtcNow,
				};
				invoice = await _invoiceRepository.CreateMinimalAsync(invoice);
			}

			// 3) Upsert Payment by PayOS order code
			if (!long.TryParse(orderCode, out var payOsOrder)) payOsOrder = booking.BookingId;
			var payment = await _paymentRepository.GetByPayOsOrderCodeAsync(payOsOrder);
			if (payment == null)
			{
				payment = new Domain.Entities.Payment
				{
					PaymentCode = $"PAY{DateTime.UtcNow:yyyyMMddHHmmss}{booking.BookingId}",
					InvoiceId = invoice.InvoiceId,
					PayOsorderCode = payOsOrder,
					Amount = (int)Math.Round((booking.TotalEstimatedCost ?? 0m)),
					Status = "PAID",
					PaidAt = DateTime.UtcNow,
					CreatedAt = DateTime.UtcNow,
					BuyerName = invoice.BillingName,
					BuyerPhone = invoice.BillingPhone,
					BuyerAddress = invoice.BillingAddress
				};
				await _paymentRepository.CreateAsync(payment);
			}
			else
			{
				payment.Status = "PAID";
				payment.PaidAt = DateTime.UtcNow;
				await _paymentRepository.UpdateAsync(payment);
			}
		}
		else if (status == "CANCELLED" || status == "FAILED" || status == "EXPIRED")
		{
			booking.Status = "CANCELLED";
		}
		else
		{
			return false;
		}

		await _bookingRepository.UpdateBookingAsync(booking);
		return true;
	}

	
}


