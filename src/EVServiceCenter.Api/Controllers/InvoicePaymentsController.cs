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

    // ============================================
    // ADMIN ENDPOINTS
    // ============================================

    /// <summary>
    /// Admin: Lấy danh sách invoices với pagination, filter, search, sort
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> ListForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? customerId = null,
        [FromQuery] int? bookingId = null,
        [FromQuery] int? orderId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc")
    {
        // Check ModelState for binding errors
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToList();
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
        }

        try
        {
            if (page < 1) return BadRequest(new { success = false, message = "Page phải lớn hơn 0" });
            if (pageSize < 1 || pageSize > 100) return BadRequest(new { success = false, message = "Page size phải từ 1 đến 100" });

            var (items, totalCount) = await _invoiceRepo.QueryForAdminAsync(
                page, pageSize, customerId, bookingId, orderId, status, from, to, searchTerm, sortBy, sortOrder);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Project to DTOs to avoid circular reference issues
            var data = items.Select(i => new
            {
                invoiceId = i.InvoiceId,
                customerId = i.CustomerId,
                bookingId = i.BookingId,
                orderId = i.OrderId,
                status = i.Status,
                email = i.Email,
                phone = i.Phone,
                packageDiscountAmount = i.PackageDiscountAmount,
                promotionDiscountAmount = i.PromotionDiscountAmount,
                partsAmount = i.PartsAmount,
                createdAt = i.CreatedAt,
                customer = i.Customer != null ? new
                {
                    customerId = i.Customer.CustomerId,
                    user = i.Customer.User != null ? new
                    {
                        userId = i.Customer.User.UserId,
                        fullName = i.Customer.User.FullName,
                        email = i.Customer.User.Email,
                        phoneNumber = i.Customer.User.PhoneNumber
                    } : null
                } : null,
                booking = i.Booking != null ? new
                {
                    bookingId = i.Booking.BookingId,
                    serviceId = i.Booking.ServiceId,
                    centerId = i.Booking.CenterId
                } : null,
				order = i.Order != null ? new
				{
					orderId = i.Order.OrderId,
					payOSOrderCode = i.Order.PayOSOrderCode
				} : null,
                paymentsCount = i.Payments?.Count ?? 0,
                totalPaid = i.Payments?.Where(p => p.Status == "PAID" || p.Status == "COMPLETED").Sum(p => (long)p.Amount) ?? 0
            }).ToList();

            var pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(new { success = true, data = data, pagination });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách invoices: {ex.Message}" });
        }
    }

    /// <summary>
    /// Admin: Lấy thống kê invoices
    /// </summary>
    [HttpGet("admin/stats")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? centerId = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            // Get all invoices in date range
            var allInvoices = await _invoiceRepo.GetAllAsync();
            allInvoices = allInvoices.Where(i => i.CreatedAt >= fromDate && i.CreatedAt <= toDate).ToList();

            // Filter by center if specified
            if (centerId.HasValue)
            {
                allInvoices = allInvoices.Where(i =>
                    (i.Booking?.CenterId == centerId.Value) ||
                    (i.Order?.FulfillmentCenterId == centerId.Value)
                ).ToList();
            }

            var total = allInvoices.Count;
            var paid = allInvoices.Count(i => i.Status == "PAID");
            var pending = allInvoices.Count(i => i.Status == "PENDING");
            var cancelled = allInvoices.Count(i => i.Status == "CANCELLED");

            // By source
            var bySource = new
            {
                booking = allInvoices.Count(i => i.BookingId.HasValue),
                order = allInvoices.Count(i => i.OrderId.HasValue)
            };

            // Today, this month, this year
            var today = DateTime.UtcNow.Date;
            var todayInvoices = allInvoices.Where(i => i.CreatedAt.Date == today).ToList();
            var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var thisMonthInvoices = allInvoices.Where(i => i.CreatedAt >= thisMonth).ToList();
            var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
            var thisYearInvoices = allInvoices.Where(i => i.CreatedAt >= thisYear).ToList();

            // Calculate total amounts - Đúng: ServicePrice + PartsAmount - PackageDiscountAmount - PromotionDiscountAmount
            // ServicePrice lấy từ Booking.Service.BasePrice
            var totalAmount = allInvoices.Sum(i =>
            {
                var servicePrice = i.Booking?.Service?.BasePrice ?? 0m;
                var finalServicePrice = servicePrice - i.PackageDiscountAmount; // Giá dịch vụ sau khi trừ discount gói
                return finalServicePrice + i.PartsAmount - i.PromotionDiscountAmount;
            });
            var paidAmount = allInvoices.Where(i => i.Status == "PAID").Sum(i =>
            {
                var servicePrice = i.Booking?.Service?.BasePrice ?? 0m;
                var finalServicePrice = servicePrice - i.PackageDiscountAmount; // Giá dịch vụ sau khi trừ discount gói
                return finalServicePrice + i.PartsAmount - i.PromotionDiscountAmount;
            });

            var stats = new
            {
                total,
                totalAmount,
                paid,
                paidAmount,
                pending,
                cancelled,
                bySource,
                today = new { count = todayInvoices.Count, amount = todayInvoices.Sum(i => {
                    var servicePrice = i.Booking?.Service?.BasePrice ?? 0m;
                    var finalServicePrice = servicePrice - i.PackageDiscountAmount;
                    return finalServicePrice + i.PartsAmount - i.PromotionDiscountAmount;
                }) },
                thisMonth = new { count = thisMonthInvoices.Count, amount = thisMonthInvoices.Sum(i => {
                    var servicePrice = i.Booking?.Service?.BasePrice ?? 0m;
                    var finalServicePrice = servicePrice - i.PackageDiscountAmount;
                    return finalServicePrice + i.PartsAmount - i.PromotionDiscountAmount;
                }) },
                thisYear = new { count = thisYearInvoices.Count, amount = thisYearInvoices.Sum(i => {
                    var servicePrice = i.Booking?.Service?.BasePrice ?? 0m;
                    var finalServicePrice = servicePrice - i.PackageDiscountAmount;
                    return finalServicePrice + i.PartsAmount - i.PromotionDiscountAmount;
                }) }
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thống kê invoices: {ex.Message}" });
        }
    }

    /// <summary>
    /// Admin: Lấy chi tiết invoice (cải thiện)
    /// </summary>
    [HttpGet("/api/invoices/{invoiceId:int}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceById([FromRoute] int invoiceId)
    {
        try
        {
            var inv = await _invoiceRepo.GetByIdWithDetailsAsync(invoiceId);
            if (inv == null) return NotFound(new { success = false, message = "Không tìm thấy hóa đơn" });

            // Project to DTO to avoid circular reference
            var data = new
            {
                invoiceId = inv.InvoiceId,
                customerId = inv.CustomerId,
                bookingId = inv.BookingId,
                orderId = inv.OrderId,
                status = inv.Status,
                email = inv.Email,
                phone = inv.Phone,
                packageDiscountAmount = inv.PackageDiscountAmount,
                promotionDiscountAmount = inv.PromotionDiscountAmount,
                partsAmount = inv.PartsAmount,
                createdAt = inv.CreatedAt,
                customer = inv.Customer != null ? new
                {
                    customerId = inv.Customer.CustomerId,
                    user = inv.Customer.User != null ? new
                    {
                        userId = inv.Customer.User.UserId,
                        fullName = inv.Customer.User.FullName,
                        email = inv.Customer.User.Email,
                        phoneNumber = inv.Customer.User.PhoneNumber
                    } : null
                } : null,
                booking = inv.Booking != null ? new
                {
                    bookingId = inv.Booking.BookingId,
                    serviceId = inv.Booking.ServiceId,
                    centerId = inv.Booking.CenterId,
                    service = inv.Booking.Service != null ? new
                    {
                        serviceId = inv.Booking.Service.ServiceId,
                        serviceName = inv.Booking.Service.ServiceName,
                        basePrice = inv.Booking.Service.BasePrice
                    } : null
                } : null,
				order = inv.Order != null ? new
				{
					orderId = inv.Order.OrderId,
					payOSOrderCode = inv.Order.PayOSOrderCode
				} : null,
                payments = inv.Payments?.Select(p => new
                {
                    paymentId = p.PaymentId,
                    paymentCode = p.PaymentCode,
                    amount = p.Amount,
                    status = p.Status,
                    paymentMethod = p.PaymentMethod,
                    paidByUserId = p.PaidByUserID,
                    createdAt = p.CreatedAt,
                    paidAt = p.PaidAt
                }).ToList()
            };

            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết invoice: {ex.Message}" });
        }
    }

    /// <summary>
    /// Admin: Cập nhật trạng thái invoice
    /// </summary>
    [HttpPatch("/api/invoices/{invoiceId:int}/status")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> UpdateInvoiceStatus([FromRoute] int invoiceId, [FromBody] UpdateInvoiceStatusRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { success = false, message = "Status là bắt buộc" });
            }

            var validStatuses = new[] { "PAID", "PENDING", "CANCELLED", "VOID" };
            var status = request.Status.Trim().ToUpperInvariant();
            if (!validStatuses.Contains(status))
            {
                return BadRequest(new { success = false, message = $"Status không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", validStatuses)}" });
            }

            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy invoice" });
            }

            await _invoiceRepo.UpdateStatusAsync(invoiceId, status);
            return Ok(new { success = true, message = $"Đã cập nhật trạng thái invoice thành {status}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật trạng thái invoice: {ex.Message}" });
        }
    }

    public class UpdateInvoiceStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
