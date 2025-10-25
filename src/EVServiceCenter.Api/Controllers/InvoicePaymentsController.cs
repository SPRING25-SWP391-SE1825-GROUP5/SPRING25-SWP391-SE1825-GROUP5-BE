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
        public string Note { get; set; } = string.Empty;
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
            PaidByUserID = req.PaidByUserId,
        };

        payment = await _paymentRepo.CreateAsync(payment);

        return Ok(new {
            paymentId = payment.PaymentId,
            paymentCode = payment.PaymentCode,
            status = payment.Status,
            amount = payment.Amount,
            paymentMethod = payment.PaymentMethod,
            paidByUserId = payment.PaidByUserID
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List([FromRoute] int invoiceId, [FromQuery] string? status = null, [FromQuery] string? method = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
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
            paidByUserId = p.PaidByUserID,
            createdAt = p.CreatedAt,
            paidAt = p.PaidAt
        });

        return Ok(resp);
    }

    [HttpGet("/api/invoices")]
    [Authorize]
    public async Task<IActionResult> GetAllInvoices()
    {
        var items = await _invoiceRepo.GetAllAsync();
        var resp = items.Select(i => new {
            invoiceId = i.InvoiceId,
            customerId = i.CustomerId,
            bookingId = i.BookingId,
            orderId = i.OrderId,
            status = i.Status,
            email = i.Email,
            phone = i.Phone,
            createdAt = i.CreatedAt
        });
        return Ok(resp);
    }


    [HttpGet("/api/invoices/{invoiceId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceById([FromRoute] int invoiceId)
    {
        var inv = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (inv == null) return NotFound(new { success = false, message = "Không tìm thấy hóa đơn" });
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
        
        var body = await _email.RenderInvoiceEmailTemplateAsync(
            customerName: "Khách hàng",
            invoiceId: inv.InvoiceId.ToString(),
            bookingId: inv.OrderId?.ToString() ?? "N/A",
            createdDate: inv.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            customerEmail: email,
            serviceName: "Dịch vụ",
            servicePrice: "0",
            totalAmount: "0",
            hasDiscount: false,
            discountAmount: "0"
        );
        
        await _email.SendEmailAsync(email, subject, body);
        return Ok(new { success = true, message = "Đã gửi email hóa đơn" });
    }
}