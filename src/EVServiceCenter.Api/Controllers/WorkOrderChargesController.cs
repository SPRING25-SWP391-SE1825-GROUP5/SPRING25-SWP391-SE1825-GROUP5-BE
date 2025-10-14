using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EVServiceCenter.Api.Controllers
{
[ApiController]
[Route("api/workorders/{workOrderId:int}/charges")]
[Authorize]
public class WorkOrderChargesController : ControllerBase
    {
        private readonly IWorkOrderRepository _workOrderRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly HttpClient _httpClient;
        private readonly EVServiceCenter.Application.Interfaces.IEmailService _email;
        private readonly PayOsOptions _payos;

        public WorkOrderChargesController(
            IWorkOrderRepository workOrderRepo,
            IInvoiceRepository invoiceRepo,
            IPaymentRepository paymentRepo,
            IOptions<PayOsOptions> payos,
            HttpClient httpClient,
            EVServiceCenter.Application.Interfaces.IEmailService email)
        {
            _workOrderRepo = workOrderRepo;
            _invoiceRepo = invoiceRepo;
            _paymentRepo = paymentRepo;
            _httpClient = httpClient;
            _payos = payos.Value;
            _email = email;
        }

        [HttpGet]
        public async Task<IActionResult> GetCharges(int workOrderId)
        {
            var wo = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });

            var items = (wo.WorkOrderParts ?? new System.Collections.Generic.List<Domain.Entities.WorkOrderPart>())
                .Select(p => new
                {
                    partId = p.PartId,
                    vehicleModelPartId = p.VehicleModelPartId,
                    partName = p.Part?.PartName,
                    qty = p.QuantityUsed,
                    unitPrice = p.UnitCost,
                    lineTotal = p.UnitCost * p.QuantityUsed
                })
                .ToList();

            var subtotal = items.Sum(x => x.lineTotal);
            return Ok(new { workOrderId, subtotalParts = subtotal, serviceFee = 0m, discount = 0m, tax = 0m, total = subtotal, items });
        }

        [HttpGet("/api/workorders/{workOrderId:int}/invoice")]
        public async Task<IActionResult> GetInvoice(int workOrderId)
        {
            var wo = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });
            // Dùng bookingId để lấy invoice gần nhất cho booking này
            var invoice = await _invoiceRepo.GetByBookingIdAsync(wo.BookingId);
            if (invoice == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho WO" });
            return Ok(new { success = true, data = new { invoice.InvoiceId, invoice.Status, invoice.CreatedAt } });
        }

        [HttpGet("/api/workorders/{workOrderId:int}/payments")]
        public async Task<IActionResult> GetPayments(int workOrderId)
        {
            var wo = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });
            var invoice = await _invoiceRepo.GetByBookingIdAsync(wo.BookingId);
            if (invoice == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho WO" });
            var list = await _paymentRepo.GetByInvoiceIdAsync(invoice.InvoiceId, null, null, null, null);
            var resp = list.Select(p => new { p.PaymentId, p.PaymentCode, p.PaymentMethod, p.Amount, p.Status, p.PaidAt, p.CreatedAt });
            return Ok(new { success = true, data = resp });
        }


        [HttpPost("link")]
        public async Task<IActionResult> CreatePaymentLink(int workOrderId)
        {
            var wo = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });

            var parts = wo.WorkOrderParts ?? new System.Collections.Generic.List<Domain.Entities.WorkOrderPart>();
            Console.WriteLine($"[DEBUG] WorkOrder {workOrderId} has {parts.Count} WorkOrderParts");
            
            foreach (var part in parts)
            {
                Console.WriteLine($"[DEBUG] Part {part.PartId}: UnitCost={part.UnitCost}, Quantity={part.QuantityUsed}, Total={part.UnitCost * part.QuantityUsed}");
            }

            var total = parts.Sum(p => p.UnitCost * p.QuantityUsed);
            Console.WriteLine($"[DEBUG] Total calculated: {total}");
            
            if (total <= 0) return BadRequest(new { success = false, message = "Không có chi phí phát sinh", debug = new { partsCount = parts.Count, total = total } });

            var orderCode = long.Parse($"{workOrderId}99"); // mã riêng cho phát sinh
            var amount = (int)Math.Round(total);
            var description = ($"WO-{workOrderId}-Charges").Substring(0, Math.Min(25, $"WO-{workOrderId}-Charges".Length));
            var returnUrl = _payos.ReturnUrl ?? string.Empty;
            var cancelUrl = _payos.CancelUrl ?? string.Empty;
            
            // Tạo canonical string và signature giống PaymentService
            var canonical = string.Create(System.Globalization.CultureInfo.InvariantCulture, 
                $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
            var signature = ComputeHmacSha256Hex(canonical, _payos.ChecksumKey);
            
            var payload = new
            {
                orderCode,
                amount,
                description,
                items = wo.WorkOrderParts
                    .Where(p => p.QuantityUsed > 0 && p.UnitCost > 0) // Filter out invalid items
                    .Select(p => new { name = p.Part?.PartName ?? $"Part {p.PartId}", quantity = p.QuantityUsed, price = (int)Math.Round(p.UnitCost) }),
                returnUrl,
                cancelUrl,
                signature
            };

            var url = $"{_payos.BaseUrl.TrimEnd('/')}/payment-requests";
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            request.Headers.Add("x-client-id", _payos.ClientId);
            request.Headers.Add("x-api-key", _payos.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            var json = JsonDocument.Parse(responseText).RootElement;
            
            Console.WriteLine($"[DEBUG] PayOS Response: {responseText}");
            
            if (json.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object &&
                dataElem.TryGetProperty("checkoutUrl", out var urlElem) && urlElem.ValueKind == JsonValueKind.String)
            {
                var checkoutUrl = urlElem.GetString();
                return Ok(new { checkoutUrl });
            }
            
            var message = (json.TryGetProperty("message", out var msgElem) && msgElem.ValueKind == JsonValueKind.String ? msgElem.GetString() : null)
                ?? (json.TryGetProperty("desc", out var descElem) && descElem.ValueKind == JsonValueKind.String ? descElem.GetString() : null)
                ?? "Không nhận được checkoutUrl từ PayOS";
            return BadRequest(new { success = false, message = $"Tạo link PayOS thất bại: {message}. Response: {responseText}" });
        }

        private static string ComputeHmacSha256Hex(string data, string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
            return sb.ToString();
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int workOrderId, [FromQuery] long orderCode)
        {
            var wo = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });

            var getUrl = $"{_payos.BaseUrl.TrimEnd('/')}/payment-requests/{orderCode}";
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(getUrl));
            request.Headers.Add("x-client-id", _payos.ClientId);
            request.Headers.Add("x-api-key", _payos.ApiKey);
            var resp = await _httpClient.SendAsync(request);
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var status = data.GetProperty("data").GetProperty("status").GetString();
            if (status != "PAID" && status != "SUCCESS" && status != "COMPLETED")
                return BadRequest(new { success = false, message = $"Trạng thái PayOS: {status}" });

            // Create invoice DETAIL from WorkOrderParts
            var total = (wo.WorkOrderParts ?? new System.Collections.Generic.List<Domain.Entities.WorkOrderPart>())
                .Sum(p => p.UnitCost * p.QuantityUsed);
            var invoice = new Domain.Entities.Invoice
            {
                WorkOrderId = workOrderId,
                BookingId = wo.BookingId,
                CustomerId = wo.Booking?.CustomerId,
                Email = wo.Booking?.Customer?.User?.Email,
                Phone = wo.Booking?.Customer?.User?.PhoneNumber,
                Status = "PAID",
                CreatedAt = DateTime.UtcNow,
                
            };
            invoice = await _invoiceRepo.CreateMinimalAsync(invoice);


            var payment = (Domain.Entities.Payment)null;
            if (payment == null)
            {
                payment = new Domain.Entities.Payment
                {
                    PaymentCode = $"PAY{DateTime.UtcNow:yyyyMMddHHmmss}{workOrderId}",
                    InvoiceId = invoice.InvoiceId,
                    PaymentMethod = "PAYOS",
                    Amount = (int)Math.Round(total),
                    Status = "PAID",
                    PaidAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                };
                await _paymentRepo.CreateAsync(payment);
            }
            else
            {
                payment.InvoiceId = invoice.InvoiceId;
                payment.Status = "PAID";
                payment.PaidAt = DateTime.UtcNow;
                await _paymentRepo.UpdateAsync(payment);
            }

            try
            {
                var customerEmail = wo.Booking?.Customer?.User?.Email;
                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    var pdf = BuildInvoicePdf(invoice);
                    var subject = $"Hóa đơn phát sinh #{invoice.InvoiceId}";
                    var body = $"<p>Cảm ơn bạn đã thanh toán phát sinh.</p><p>Mã hóa đơn: {invoice.InvoiceId}</p>";
                    await _email.SendEmailWithAttachmentAsync(customerEmail, subject, body, $"Invoice_{invoice.InvoiceId}.pdf", pdf);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Send invoice email failed: {ex.Message}");
            }

            return Ok(new { success = true, invoiceId = invoice.InvoiceId, status = "PAID" });
        }

        public class OfflinePaymentRequest
        {
            public int Amount { get; set; }
            public int PaidByUserId { get; set; }
            public string Note { get; set; }
        }

        [HttpPost("offline")]
        public async Task<IActionResult> CreateOffline(int workOrderId, [FromBody] OfflinePaymentRequest req)
        {
            if (req == null || req.Amount <= 0 || req.PaidByUserId <= 0)
                return BadRequest(new { success = false, message = "amount và paidByUserId là bắt buộc" });

            var wo = await _workOrderRepo.GetByIdAsync(workOrderId);
            if (wo == null) return NotFound(new { success = false, message = "WorkOrder không tồn tại" });

            // Tính tổng chi phí phát sinh
            var total = (wo.WorkOrderParts ?? new System.Collections.Generic.List<Domain.Entities.WorkOrderPart>())
                .Sum(p => p.UnitCost * p.QuantityUsed);
            if (total <= 0) total = req.Amount; // fallback theo amount nhập nếu không có parts

            // Tạo invoice DETAIL
            var invoice = new Domain.Entities.Invoice
            {
                WorkOrderId = workOrderId,
                BookingId = wo.BookingId,
                CustomerId = wo.Booking?.CustomerId,
                Email = wo.Booking?.Customer?.User?.Email,
                Phone = wo.Booking?.Customer?.User?.PhoneNumber,
                Status = "PAID",
                CreatedAt = DateTime.UtcNow,
                
            };
            invoice = await _invoiceRepo.CreateMinimalAsync(invoice);

            var payment = new Domain.Entities.Payment
            {
                PaymentCode = $"PAYCASH{DateTime.UtcNow:yyyyMMddHHmmss}{workOrderId}",
                InvoiceId = invoice.InvoiceId,
                PaymentMethod = "CASH",
                Amount = (int)Math.Round(total),
                Status = "PAID",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                PaidByUserId = req.PaidByUserId,
            };
            await _paymentRepo.CreateAsync(payment);

            return Ok(new { success = true, invoiceId = invoice.InvoiceId, paymentId = payment.PaymentId, status = payment.Status });
        }

        private static byte[] BuildInvoicePdf(Domain.Entities.Invoice invoice)
        {
            // Simple PDF using plain text (placeholder). Replace with QuestPDF if available.
            var sb = new StringBuilder();
            sb.AppendLine($"Invoice: {invoice.InvoiceId}");
            sb.AppendLine($"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Customer email: {invoice.Email}");
            sb.AppendLine("Items: (chi tiết đã giản lược)");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}


