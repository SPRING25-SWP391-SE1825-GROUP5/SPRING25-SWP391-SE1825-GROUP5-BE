using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/invoices/{invoiceId:int}/payments")]
public class InvoicePaymentsController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IInvoiceRepository _invoiceRepo;

    public InvoicePaymentsController(IPaymentRepository paymentRepo, IInvoiceRepository invoiceRepo)
    {
        _paymentRepo = paymentRepo;
        _invoiceRepo = invoiceRepo;
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
}


