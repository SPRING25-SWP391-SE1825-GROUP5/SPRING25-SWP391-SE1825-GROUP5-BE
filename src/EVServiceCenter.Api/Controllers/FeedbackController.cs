using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Interfaces;
using System.Security.Claims;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly EVServiceCenter.Infrastructure.Configurations.EVDbContext _db;
    private readonly ICustomerRepository _customerRepository;
    public FeedbackController(EVServiceCenter.Infrastructure.Configurations.EVDbContext db, ICustomerRepository customerRepository) { _db = db; _customerRepository = customerRepository; }

    /// <summary>
    /// Lấy danh sách đánh giá của customer hiện tại (tự động lấy customerId từ JWT token)
    /// </summary>
    [HttpGet("my-reviews")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Lấy customerId từ JWT token
            var customerId = GetCustomerIdFromToken();
            if (!customerId.HasValue)
            {
                return BadRequest(new { success = false, message = "Không xác định được khách hàng. Vui lòng đăng nhập lại." });
            }

            var query = _db.Feedbacks.AsNoTracking()
                .Where(x => x.CustomerId == customerId.Value)
                .OrderByDescending(x => x.CreatedAt);

            var total = await query.CountAsync();
            
            // Lấy danh sách feedback IDs trước (chỉ select FeedbackId để tránh lỗi)
            var feedbackIds = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.FeedbackId)
                .ToListAsync();

            if (feedbackIds.Count == 0)
            {
                return Ok(new 
                { 
                    success = true, 
                    message = "Lấy danh sách đánh giá thành công",
                    total,
                    page,
                    pageSize,
                    data = new List<object>()
                });
            }

            // Load feedbacks với related data
            var feedbacks = await _db.Feedbacks
                .AsNoTracking()
                .Where(x => feedbackIds.Contains(x.FeedbackId))
                .Include(x => x.Part)
                .Include(x => x.Technician)
                    .ThenInclude(t => t != null ? t.User : null!)
                .ToListAsync();

            // Map sang DTO (load Part và Technician riêng để tránh lỗi column)
            var partIds = feedbacks.Where(f => f.PartId.HasValue).Select(f => f.PartId!.Value).Distinct().ToList();
            var technicianIds = feedbacks.Where(f => f.TechnicianId.HasValue).Select(f => f.TechnicianId!.Value).Distinct().ToList();

            var parts = await _db.Parts
                .AsNoTracking()
                .Where(p => partIds.Contains(p.PartId))
                .ToDictionaryAsync(p => p.PartId);

            var technicians = await _db.Technicians
                .AsNoTracking()
                .Where(t => technicianIds.Contains(t.TechnicianId))
                .Include(t => t.User)
                .ToDictionaryAsync(t => t.TechnicianId);

            // Map sang DTO
            var data = feedbacks.OrderByDescending(x => x.CreatedAt)
                .Select(x => new 
                { 
                    feedbackId = x.FeedbackId,
                    customerId = x.CustomerId,
                    bookingId = x.BookingId,
                    orderId = x.OrderId,
                    partId = x.PartId,
                    technicianId = x.TechnicianId,
                    rating = x.Rating,
                    comment = x.Comment,
                    isAnonymous = x.IsAnonymous,
                    createdAt = x.CreatedAt,
                    // Lấy thông tin liên quan từ dictionary
                    partName = x.PartId.HasValue && parts.ContainsKey(x.PartId.Value) 
                        ? parts[x.PartId.Value].PartName 
                        : null,
                    technicianName = x.TechnicianId.HasValue && technicians.ContainsKey(x.TechnicianId.Value) 
                        ? technicians[x.TechnicianId.Value].User?.FullName 
                        : null
                })
                .ToList();

            return Ok(new 
            { 
                success = true, 
                message = "Lấy danh sách đánh giá thành công",
                total,
                page,
                pageSize,
                data 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    private int? GetCustomerIdFromToken()
    {
        // Lấy customerId từ JWT token claim
        var customerIdClaim = User.FindFirst("customerId")?.Value;
        if (int.TryParse(customerIdClaim, out int customerId))
        {
            return customerId;
        }
        return null;
    }

    [HttpPost("orders/{orderId:int}/parts/{partId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> CreateForOrderPart(int orderId, int partId, [FromBody] CreateOrderPartFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null) return BadRequest(new { success = false, message = $"orderId {orderId} không tồn tại" });
        var completedStatuses = new[] { "COMPLETED", "DONE", "FINISHED" };
        if (string.IsNullOrWhiteSpace(order.Status) || !completedStatuses.Contains(order.Status.ToUpper()))
        {
            return BadRequest(new { success = false, message = "Chỉ được đánh giá sau khi đơn hàng đã hoàn thành" });
        }
        var partExists = await _db.Parts.AsNoTracking().AnyAsync(p => p.PartId == partId);
        if (!partExists) return BadRequest(new { success = false, message = $"partId {partId} không tồn tại" });
        var partInOrder = await _db.OrderItems.AsNoTracking().AnyAsync(oi => oi.OrderId == orderId && oi.PartId == partId);
        if (!partInOrder) return BadRequest(new { success = false, message = "Phụ tùng không thuộc đơn hàng này" });
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null) return BadRequest(new { success = false, message = $"customerId {request.CustomerId} không tồn tại" });

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = orderId,
            BookingId = null,
            PartId = partId,
            TechnicianId = null,
            Rating = (byte)request.Rating,
            Comment = request.Comment?.Trim(),
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };
        _db.Feedbacks.Add(fb);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { fb.FeedbackId } });
    }

    [HttpPost("bookings/{bookingId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> CreateForBooking(int bookingId, [FromBody] CreateBookingFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        
        var booking = await _db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.BookingId == bookingId);
        if (booking == null) return BadRequest(new { success = false, message = $"bookingId {bookingId} không tồn tại" });
        
        var completedStatuses = new[] { "COMPLETED", "DONE", "FINISHED" };
        if (string.IsNullOrWhiteSpace(booking.Status) || !completedStatuses.Contains(booking.Status.ToUpper()))
        {
            return BadRequest(new { success = false, message = "Chỉ được đánh giá sau khi booking đã hoàn thành" });
        }
        
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null) return BadRequest(new { success = false, message = $"customerId {request.CustomerId} không tồn tại" });

        if (request.TechnicianId.HasValue)
        {
            var techExists = await _db.Technicians.AsNoTracking().AnyAsync(t => t.TechnicianId == request.TechnicianId.Value);
            if (!techExists) return BadRequest(new { success = false, message = $"technicianId {request.TechnicianId} không tồn tại" });
            
            if (booking.TechnicianTimeSlot?.TechnicianId != request.TechnicianId.Value)
            {
                return BadRequest(new { success = false, message = "Kỹ thuật viên không phụ trách booking này" });
            }
        }

        if (request.PartId.HasValue)
        {
            var partExists = await _db.Parts.AsNoTracking().AnyAsync(p => p.PartId == request.PartId.Value);
            if (!partExists) return BadRequest(new { success = false, message = $"partId {request.PartId} không tồn tại" });
            
            var partInBooking = await _db.WorkOrderParts.AsNoTracking().AnyAsync(wop => wop.BookingId == bookingId && wop.PartId == request.PartId.Value);
            if (!partInBooking) return BadRequest(new { success = false, message = "Phụ tùng không thuộc booking này" });
        }

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = null,
            BookingId = bookingId,
            PartId = request.PartId,
            TechnicianId = request.TechnicianId,
            Rating = (byte)request.Rating,
            Comment = request.Comment?.Trim(),
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Feedbacks.Add(fb);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { fb.FeedbackId } });
    }

    [HttpPut("{feedbackId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> Update(int feedbackId, [FromBody] UpdateFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        var fb = await _db.Feedbacks.FirstOrDefaultAsync(x => x.FeedbackId == feedbackId);
        if (fb == null) return NotFound(new { success = false, message = "Feedback không tồn tại" });
        var isAdmin = User.IsInRole("Admin") || User.Claims.Any(c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && string.Equals(c.Value, "ADMIN", StringComparison.OrdinalIgnoreCase));
        if (!isAdmin)
        {
            var nameId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "userId");
            if (nameId == null || !int.TryParse(nameId.Value, out var userId))
            {
                return Forbid();
            }
            var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null || fb.CustomerId != customer.CustomerId)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền cập nhật feedback này" });
            }
        }
        fb.Rating = (byte)request.Rating;
        fb.Comment = request.Comment?.Trim();
        fb.IsAnonymous = request.IsAnonymous;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{feedbackId:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int feedbackId)
    {
        var fb = await _db.Feedbacks.FirstOrDefaultAsync(x => x.FeedbackId == feedbackId);
        if (fb == null) return NotFound(new { success = false, message = "Feedback không tồn tại" });
        _db.Feedbacks.Remove(fb);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] int? customerId, [FromQuery] int? partId, [FromQuery] int? technicianId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var q = _db.Feedbacks.AsNoTracking().AsQueryable();
        if (customerId.HasValue) q = q.Where(x => x.CustomerId == customerId);
        if (partId.HasValue) q = q.Where(x => x.PartId == partId);
        if (technicianId.HasValue) q = q.Where(x => x.TechnicianId == technicianId);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to);
        var total = await q.CountAsync();
        var data = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.FeedbackId, x.CustomerId, x.PartId, x.TechnicianId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt })
            .ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data });
    }

    [HttpGet("{feedbackId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int feedbackId)
    {
        var fb = await _db.Feedbacks.AsNoTracking().Where(x => x.FeedbackId == feedbackId)
            .Select(x => new { x.FeedbackId, x.CustomerId, x.PartId, x.TechnicianId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.OrderId, x.BookingId })
            .FirstOrDefaultAsync();
        if (fb == null) return NotFound(new { success = false, message = "Feedback không tồn tại" });
        return Ok(new { success = true, data = fb });
    }

    [HttpGet("parts/{partId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListByPart(int partId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var q = _db.Feedbacks.AsNoTracking().Where(x => x.PartId == partId);
        var total = await q.CountAsync();
        var data = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.FeedbackId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.CustomerId })
            .ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data });
    }

    [HttpGet("technicians/{technicianId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListByTechnician(int technicianId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var q = _db.Feedbacks.AsNoTracking().Where(x => x.TechnicianId == technicianId);
        var total = await q.CountAsync();
        var data = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.FeedbackId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.CustomerId })
            .ToListAsync();
        return Ok(new { success = true, total, page, pageSize, data });
    }

    [HttpGet("parts/{partId:int}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> PartSummary(int partId)
    {
        var q = _db.Feedbacks.AsNoTracking().Where(x => x.PartId == partId);
        var count = await q.CountAsync();
        var avg = count == 0 ? 0 : await q.AverageAsync(x => (double)x.Rating);
        return Ok(new { success = true, data = new { avgRating = Math.Round(avg,2), count } });
    }

    [HttpGet("technicians/{technicianId:int}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> TechnicianSummary(int technicianId)
    {
        var q = _db.Feedbacks.AsNoTracking().Where(x => x.TechnicianId == technicianId);
        var count = await q.CountAsync();
        var avg = count == 0 ? 0 : await q.AverageAsync(x => (double)x.Rating);
        return Ok(new { success = true, data = new { avgRating = Math.Round(avg,2), count } });
    }

    [HttpPost("parts/{partId:int}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicForPart(int partId, [FromBody] PublicPartFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        var partExists = await _db.Parts.AsNoTracking().AnyAsync(p => p.PartId == partId);
        if (!partExists) return BadRequest(new { success = false, message = $"partId {partId} không tồn tại" });

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = null,
            BookingId = null,
            PartId = partId,
            TechnicianId = null,
            Rating = (byte)request.Rating,
            Comment = request.Comment?.Trim(),
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };
        _db.Feedbacks.Add(fb);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { fb.FeedbackId } });
    }

    [HttpPost("technicians/{technicianId:int}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicForTechnician(int technicianId, [FromBody] PublicTechnicianFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        var techExists = await _db.Technicians.AsNoTracking().AnyAsync(t => t.TechnicianId == technicianId);
        if (!techExists) return BadRequest(new { success = false, message = $"technicianId {technicianId} không tồn tại" });

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = null,
            BookingId = null,
            PartId = null,
            TechnicianId = technicianId,
            Rating = (byte)request.Rating,
            Comment = request.Comment?.Trim(),
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };
        _db.Feedbacks.Add(fb);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { fb.FeedbackId } });
    }

    [HttpGet("orders/{orderId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> ListByOrder(int orderId)
    {
        var data = await _db.Feedbacks.AsNoTracking().Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.FeedbackId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.PartId, x.TechnicianId })
            .ToListAsync();
        return Ok(new { success = true, data });
    }

    [HttpGet("bookings/{bookingId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> ListByBooking(int bookingId)
    {
        var data = await _db.Feedbacks.AsNoTracking().Where(x => x.BookingId == bookingId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.FeedbackId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.PartId, x.TechnicianId })
            .ToListAsync();
        return Ok(new { success = true, data });
    }
}