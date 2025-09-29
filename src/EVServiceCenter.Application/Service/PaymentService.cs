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
    private readonly IEmailService _emailService;
    private readonly IServicePartRepository _servicePartRepository;
    private readonly IWorkOrderPartRepository _workOrderPartRepository;
    private readonly IMaintenanceChecklistRepository _checklistRepository;
    private readonly IMaintenanceChecklistResultRepository _checklistResultRepository;

    public PaymentService(HttpClient httpClient, IOptions<PayOsOptions> options, IBookingRepository bookingRepository, IWorkOrderRepository workOrderRepository, IInvoiceRepository invoiceRepository, IPaymentRepository paymentRepository, ITechnicianRepository technicianRepository, IEmailService emailService, IServicePartRepository servicePartRepository, IWorkOrderPartRepository workOrderPartRepository, IMaintenanceChecklistRepository checklistRepository, IMaintenanceChecklistResultRepository checklistResultRepository)
	{
		_httpClient = httpClient;
		_options = options.Value;
		_bookingRepository = bookingRepository;
		_workOrderRepository = workOrderRepository;
		_invoiceRepository = invoiceRepository;
		_paymentRepository = paymentRepository;
        _technicianRepository = technicianRepository;
        _emailService = emailService;
        _servicePartRepository = servicePartRepository;
        _workOrderPartRepository = workOrderPartRepository;
        _checklistRepository = checklistRepository;
        _checklistResultRepository = checklistResultRepository;
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

		Console.WriteLine($"[DEBUG] ConfirmPaymentAsync called with orderCode: {orderCode}");

		var getUrl = $"{_options.BaseUrl.TrimEnd('/')}/payment-requests/{orderCode}";
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(getUrl));
		request.Headers.Add("x-client-id", _options.ClientId);
		request.Headers.Add("x-api-key", _options.ApiKey);

		var response = await _httpClient.SendAsync(request);
		response.EnsureSuccessStatusCode();
		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		var status = json.GetProperty("data").GetProperty("status").GetString();
		
		Console.WriteLine($"[DEBUG] PayOS status for orderCode {orderCode}: {status}");

		Domain.Entities.Booking booking = null;
		if (int.TryParse(orderCode, out var bookingIdFromOrder))
		{
			booking = await _bookingRepository.GetBookingByIdAsync(bookingIdFromOrder);
		}
		if (booking == null)
		{
			booking = await _bookingRepository.GetBookingByCodeAsync(orderCode);
		}
		if (booking == null) 
		{
			Console.WriteLine($"[DEBUG] Booking not found for orderCode: {orderCode}");
			return false;
		}

		Console.WriteLine($"[DEBUG] Found booking {booking.BookingId} with status: {booking.Status}");

		Domain.Entities.Invoice emailInvoiceRef = null;

		if (status == "PAID" || status == "SUCCESS" || status == "COMPLETED")
		{
			Console.WriteLine($"[DEBUG] Payment successful, updating booking {booking.BookingId} to CONFIRMED");
			booking.Status = "CONFIRMED";

            // 1) Ensure WorkOrder exists
            var workOrder = await _workOrderRepository.GetByBookingIdAsync(booking.BookingId);
            if (workOrder == null)
            {
                // Ưu tiên kỹ thuật viên đã gán ở Booking; nếu chưa có thì mới tự chọn
                var techId = booking.TechnicianId ?? 0;
                if (techId == 0)
                {
                    var techsInCenter = await _technicianRepository.GetTechniciansByCenterIdAsync(booking.CenterId);
                    if (techsInCenter != null && techsInCenter.Count > 0) techId = techsInCenter[0].TechnicianId;
                    else
                    {
                        var allTechs = await _technicianRepository.GetAllTechniciansAsync();
                        if (allTechs != null && allTechs.Count > 0) techId = allTechs[0].TechnicianId;
                    }
                }
                if (techId == 0) throw new InvalidOperationException("Không có kỹ thuật viên để lập WorkOrder");
                workOrder = new Domain.Entities.WorkOrder
				{
					WorkOrderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{booking.BookingId}",
					BookingId = booking.BookingId,
                    TechnicianId = techId,
                    Status = "NOT_STARTED",
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
				Console.WriteLine($"[DEBUG] Creating new invoice for booking {booking.BookingId}");
				invoice = new Domain.Entities.Invoice
				{
					InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{workOrder.WorkOrderId}",
					WorkOrderId = workOrder.WorkOrderId,
					BookingId = booking.BookingId,
					CustomerId = booking.CustomerId,
					BillingName = booking.Customer?.User?.FullName ?? "Guest",
					BillingPhone = booking.Customer?.User?.PhoneNumber,
					BillingAddress = booking.Center?.Address,
					Status = "PAID",
					TotalAmount = booking.TotalEstimatedCost ?? 0m,
					CreatedAt = DateTime.UtcNow,
				};
				invoice = await _invoiceRepository.CreateMinimalAsync(invoice);
				Console.WriteLine($"[DEBUG] Invoice created with ID: {invoice.InvoiceId}");
				
				// Tạo InvoiceItems từ BookingServices
				await CreateInvoiceItemsFromBookingServicesAsync(invoice, booking);
			}
			else
			{
				Console.WriteLine($"[DEBUG] Invoice already exists with ID: {invoice.InvoiceId}");
			}

			// Giữ tham chiếu để gửi email sau khi cập nhật booking
			emailInvoiceRef = invoice;

            // 3) Clone ServiceParts -> WorkOrderParts (checklist/gợi ý)
            try
            {
                if (workOrder != null && booking.ServiceId > 0)
                {
                    var templates = await _servicePartRepository.GetByServiceIdAsync(booking.ServiceId);
                    var existing = await _workOrderPartRepository.GetByWorkOrderIdAsync(workOrder.WorkOrderId);
                    var existingPartIds = existing.Select(e => e.PartId).ToHashSet();

                    foreach (var t in templates)
                    {
                        if (!existingPartIds.Contains(t.PartId))
                        {
                            await _workOrderPartRepository.AddAsync(new Domain.Entities.WorkOrderPart
                            {
                                WorkOrderId = workOrder.WorkOrderId,
                                PartId = t.PartId,
                                QuantityUsed = 0,
                                UnitCost = 0,
                                TotalCost = 0
                            });
                        }
                    }

                    // 3b) Auto-init maintenance checklist for this work order if not exists
                    var existingChecklist = await _checklistRepository.GetByWorkOrderIdAsync(workOrder.WorkOrderId);
                    if (existingChecklist == null)
                    {
                        var checklist = await _checklistRepository.CreateAsync(new Domain.Entities.MaintenanceChecklist
                        {
                            WorkOrderId = workOrder.WorkOrderId,
                            CreatedAt = DateTime.UtcNow,
                            Notes = null
                        });

                        // Create checklist results from ServiceParts
                        var serviceParts = await _servicePartRepository.GetByServiceIdAsync(booking.ServiceId);
                        var results = serviceParts.Select(sp => new Domain.Entities.MaintenanceChecklistResult
                        {
                            ChecklistId = checklist.ChecklistId,
                            PartId = sp.PartId,
                            Description = sp.Notes ?? sp.Part?.PartName ?? $"Part {sp.PartId}",
                            IsMandatory = true,
                            Performed = false,
                            Result = null,
                            Comment = null
                        });
                        await _checklistResultRepository.UpsertManyAsync(results);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Clone ServiceParts -> WorkOrderParts failed: {ex.Message}");
            }

            // 4) Upsert Payment by PayOS order code
			if (!long.TryParse(orderCode, out var payOsOrder)) payOsOrder = booking.BookingId;
			var payment = await _paymentRepository.GetByPayOsOrderCodeAsync(payOsOrder);
			if (payment == null)
			{
				Console.WriteLine($"[DEBUG] Creating new payment for booking {booking.BookingId}");
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
				Console.WriteLine($"[DEBUG] Payment created successfully");
			}
			else
			{
				Console.WriteLine($"[DEBUG] Payment already exists, updating status to PAID");
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
        Console.WriteLine($"[DEBUG] Booking {booking.BookingId} updated successfully");

		// Send invoice email (summary + items)
        try
        {
            var customerEmail = booking.Customer?.User?.Email ?? booking.Customer?.Email;
            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
				// Đảm bảo invoice đã có (nếu không có từ trước, lấy lại theo booking)
				var emailInvoice = emailInvoiceRef ?? await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
                if (emailInvoice == null)
                {
                    Console.WriteLine("[WARN] Không tìm thấy invoice để gửi email");
                }
                else
                {
                    var subject = $"Hóa đơn thanh toán #{emailInvoice.InvoiceNumber} - EV Service Center";
                var body = $@"<h3>Cảm ơn bạn đã thanh toán!</h3>
<p><b>Mã hóa đơn:</b> {emailInvoice.InvoiceNumber}</p>
<p><b>Tổng tiền:</b> {emailInvoice.TotalAmount:N0} VND</p>
<p><b>Khách hàng:</b> {emailInvoice.BillingName}</p>
<p><b>SĐT:</b> {emailInvoice.BillingPhone}</p>
<p><b>Địa chỉ:</b> {emailInvoice.BillingAddress}</p>
<hr/>
<p>Chi tiết dịch vụ đã đặt sẽ hiển thị trên hóa đơn chi tiết trong hệ thống.</p>";
                    await _emailService.SendEmailAsync(customerEmail, subject, body);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Gửi email hóa đơn thất bại: {ex.Message}");
        }
		return true;
	}

private async Task CreateInvoiceItemsFromBookingServicesAsync(Domain.Entities.Invoice invoice, Domain.Entities.Booking booking)
	{
		try
		{
			Console.WriteLine($"[DEBUG] Creating InvoiceItems for invoice {invoice.InvoiceId}");
        var invoiceItems = new List<Domain.Entities.InvoiceItem>();

        // Mô hình 1 booking = 1 service: tạo 1 dòng invoice item từ booking.Service
        var description = booking.Service?.ServiceName ?? "Dịch vụ";
        var unitPrice = booking.TotalEstimatedCost ?? 0m;
        var item = new Domain.Entities.InvoiceItem
        {
            InvoiceId = invoice.InvoiceId,
            PartId = null,
            Description = description,
            Quantity = 1,
            UnitPrice = unitPrice,
            LineTotal = unitPrice
        };
        invoiceItems.Add(item);

        await _invoiceRepository.CreateInvoiceItemsAsync(invoiceItems);
        Console.WriteLine($"[DEBUG] Created {invoiceItems.Count} InvoiceItems successfully");
		}
		catch (Exception ex)
		{
                Console.WriteLine($"[ERROR] Failed to create InvoiceItems: {ex.Message}");
                throw;
		}
	}

	
}


