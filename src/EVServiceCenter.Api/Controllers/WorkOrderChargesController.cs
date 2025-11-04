using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.Controllers
{
[ApiController]
[Route("api/bookings/{bookingId:int}/charges")]
[Authorize]
public class WorkOrderChargesController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly HttpClient _httpClient;
        private readonly EVServiceCenter.Application.Interfaces.IEmailService _email;
        private readonly PayOsOptions _payos;
        private readonly ILogger<WorkOrderChargesController> _logger;

        public WorkOrderChargesController(
            IBookingRepository bookingRepo,
            IInvoiceRepository invoiceRepo,
            IPaymentRepository paymentRepo,
            IOptions<PayOsOptions> payos,
            HttpClient httpClient,
            EVServiceCenter.Application.Interfaces.IEmailService email,
            ILogger<WorkOrderChargesController> logger)
        {
            _bookingRepo = bookingRepo;
            _invoiceRepo = invoiceRepo;
            _paymentRepo = paymentRepo;
            _httpClient = httpClient;
            _payos = payos.Value;
            _email = email;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCharges(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

            // WorkOrder functionality merged into Booking - get parts from WorkOrderParts table using bookingId
            // This would need to be implemented in WorkOrderPartRepository if needed
            // For now, return empty items
            var items = new List<object>();

            var subtotal = 0m; // No parts available yet
            return Ok(new { bookingId, subtotalParts = subtotal, serviceFee = 0m, discount = 0m, tax = 0m, total = subtotal, items });
        }

        [HttpGet("/api/bookings/{bookingId:int}/invoice")]
        public async Task<IActionResult> GetInvoice(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            // Dùng bookingId để lấy invoice gần nhất cho booking này
            var invoice = await _invoiceRepo.GetByBookingIdAsync(bookingId);
            if (invoice == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho booking" });
            return Ok(new { success = true, data = new { invoice.InvoiceId, invoice.Status, invoice.CreatedAt } });
        }

        [HttpGet("/api/bookings/{bookingId:int}/payments")]
        public async Task<IActionResult> GetPayments(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });
            var invoice = await _invoiceRepo.GetByBookingIdAsync(bookingId);
            if (invoice == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho booking" });
            var list = await _paymentRepo.GetByInvoiceIdAsync(invoice.InvoiceId, null, null, null, null);
            var resp = list.Select(p => new { p.PaymentId, p.PaymentCode, p.PaymentMethod, p.Amount, p.Status, p.PaidAt, p.CreatedAt });
            return Ok(new { success = true, data = resp });
        }


        // Removed: duplicate and incomplete payment link creation.

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int bookingId, [FromQuery] long orderCode)
        {
            var booking = await _bookingRepo.GetBookingByIdAsync(bookingId);
            if (booking == null) return NotFound(new { success = false, message = "Booking không tồn tại" });

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

            // WorkOrder functionality merged into Booking - no separate work order needed
            var total = 0m; // No parts available yet
            var invoice = new Domain.Entities.Invoice
            {
                BookingId = bookingId,
                CustomerId = booking.CustomerId,
                Email = booking.Customer?.User?.Email,
                Phone = booking.Customer?.User?.PhoneNumber,
                Status = "PAID",
                CreatedAt = DateTime.UtcNow,

            };
            invoice = await _invoiceRepo.CreateMinimalAsync(invoice);


            var payment = (Domain.Entities.Payment?)null;
            if (payment == null)
            {
                payment = new Domain.Entities.Payment
                {
                    PaymentCode = $"PAY{DateTime.UtcNow:yyyyMMddHHmmss}{bookingId}",
                    InvoiceId = invoice.InvoiceId,
                    PaymentMethod = "PAYOS",
                    Amount = (int)Math.Round((decimal)total),
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
                var customerEmail = booking.Customer?.User?.Email;
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
                _logger.LogWarning("Send invoice email failed: {Error}", ex.Message);
            }

            return Ok(new { success = true, invoiceId = invoice.InvoiceId, status = "PAID" });
        }

        public class OfflinePaymentRequest
        {
            public int Amount { get; set; }
            public int PaidByUserId { get; set; }
            public string Note { get; set; } = string.Empty;
        }

        // Removed: offline payment for booking (not used in current flow).

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


