using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Application.Interfaces;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/invoices/{invoiceId:int}/payments")]
public class InvoicePaymentsController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IEmailService _email;

    public InvoicePaymentsController(IPaymentRepository paymentRepo, IInvoiceRepository invoiceRepo, ICustomerRepository customerRepo, IEmailService email)
    {
        _paymentRepo = paymentRepo;
        _invoiceRepo = invoiceRepo;
        _customerRepo = customerRepo;
        _email = email;
    }

    public class CreateOfflinePaymentRequest
    {
        public int Amount { get; set; }
        public int PaidByUserId { get; set; }
        public string Note { get; set; }
    }

    [HttpPost("offline")]
    [Authorize]
    public async Task<IActionResult> CreateOffline([FromRoute] int invoiceId, [FromBody] CreateOfflinePaymentRequest req)
    {
        if (req == null || req.Amount <= 0 || req.PaidByUserId <= 0)
        {
            return BadRequest(new { success = false, message = "amount và paidByUserId là bắt buộc" });
        }

        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy invoice" });
        }

        var payment = new Payment
        {
            PaymentCode = $"PAYCASH{DateTime.UtcNow:yyyyMMddHHmmss}{invoiceId}",
            InvoiceId = invoiceId,
            PaymentMethod = "CASH",
            Amount = req.Amount,
            Status = "PAID",
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PaidByUserId = req.PaidByUserId,
        };

        payment = await _paymentRepo.CreateAsync(payment);

        return Ok(new {
            paymentId = payment.PaymentId,
            paymentCode = payment.PaymentCode,
            status = payment.Status,
            amount = payment.Amount,
            paymentMethod = payment.PaymentMethod,
            paidByUserId = payment.PaidByUserId
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List([FromRoute] int invoiceId, [FromQuery] string status = null, [FromQuery] string method = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy invoice" });
        }

        var items = await _paymentRepo.GetByInvoiceIdAsync(invoiceId, status, method, from, to);
        var resp = items.Select(p => new {
            paymentId = p.PaymentId,
            paymentCode = p.PaymentCode,
            status = p.Status,
            paymentMethod = p.PaymentMethod,
            amount = p.Amount,
            paidByUserId = p.PaidByUserId,
            createdAt = p.CreatedAt,
            paidAt = p.PaidAt
        });

        return Ok(resp);
    }

    // GET api/invoices (tất cả hóa đơn)
    [HttpGet("/api/invoices")]
    [Authorize]
    public async Task<IActionResult> GetAllInvoices()
    {
        var items = await _invoiceRepo.GetAllAsync();
        var resp = items.Select(i => new {
            invoiceId = i.InvoiceId,
            customerId = i.CustomerId,
            bookingId = i.BookingId,
            workOrderId = i.WorkOrderId,
            orderId = i.OrderId,
            status = i.Status,
            email = i.Email,
            phone = i.Phone,
            createdAt = i.CreatedAt
        });
        return Ok(resp);
    }

    // GET api/invoices/customers/{customerId}
    [HttpGet("/api/invoices/customers/{customerId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoicesByCustomer([FromRoute] int customerId)
    {
        var items = await _invoiceRepo.GetByCustomerIdAsync(customerId);
        var resp = items.Select(i => new {
            invoiceId = i.InvoiceId,
            customerId = i.CustomerId,
            bookingId = i.BookingId,
            workOrderId = i.WorkOrderId,
            orderId = i.OrderId,
            status = i.Status,
            email = i.Email,
            phone = i.Phone,
            createdAt = i.CreatedAt
        });
        return Ok(resp);
    }

    // ----------- Invoice details & finders -----------
    [HttpGet("/api/invoices/{invoiceId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceById([FromRoute] int invoiceId)
    {
        var inv = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (inv == null) return NotFound(new { success = false, message = "Không tìm thấy hóa đơn" });
        return Ok(new { success = true, data = inv });
    }

    [HttpGet("/api/invoices/by-booking/{bookingId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceByBooking([FromRoute] int bookingId)
    {
        var inv = await _invoiceRepo.GetByBookingIdAsync(bookingId);
        if (inv == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho booking" });
        return Ok(new { success = true, data = inv });
    }

    [HttpGet("/api/invoices/by-workorder/{workOrderId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceByWorkOrder([FromRoute] int workOrderId)
    {
        var inv = await _invoiceRepo.GetByWorkOrderIdAsync(workOrderId);
        if (inv == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho workorder" });
        return Ok(new { success = true, data = inv });
    }

    [HttpGet("/api/invoices/by-order/{orderId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceByOrder([FromRoute] int orderId)
    {
        var inv = await _invoiceRepo.GetByOrderIdAsync(orderId);
        if (inv == null) return NotFound(new { success = false, message = "Chưa có hóa đơn cho order" });
        return Ok(new { success = true, data = inv });
    }

    [HttpPost("/api/invoices/{invoiceId:int}/send")]
    [Authorize]
    public async Task<IActionResult> SendInvoice([FromRoute] int invoiceId)
    {
        var inv = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (inv == null) return NotFound(new { success = false, message = "Không tìm thấy hóa đơn" });
        var email = inv.Email;
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { success = false, message = "Hóa đơn không có email" });
        var subject = $"Hóa đơn #{inv.InvoiceId}";
        var body = $"<p>Xin chào, hóa đơn của bạn đã được phát hành.</p>";
        await _email.SendEmailAsync(email, subject, body);
        return Ok(new { success = true, message = "Đã gửi email hóa đơn" });
    }
}


