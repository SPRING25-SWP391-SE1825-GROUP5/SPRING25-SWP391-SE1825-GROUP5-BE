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
                    .ThenInclude(t => t!.User)
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
                    bookingId = (int?)null, // BookingID column does not exist in database
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
            // BookingId = null, // BookingID column does not exist in database - ignored by EF Core
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
            // BookingId = bookingId, // BookingID column does not exist in database - ignored by EF Core
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
            .Select(x => new { x.FeedbackId, x.CustomerId, x.PartId, x.TechnicianId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.OrderId, BookingId = (int?)null }) // BookingID column does not exist in database
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
            // BookingId = null, // BookingID column does not exist in database - ignored by EF Core
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
            // BookingId = null, // BookingID column does not exist in database - ignored by EF Core
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
    public Task<IActionResult> ListByBooking(int bookingId)
    {
        // BookingID column does not exist in database - return empty list
        // This endpoint is kept for API compatibility but will always return empty
        return Task.FromResult<IActionResult>(Ok(new { success = true, data = new List<object>() }));
    }

    /// <summary>
    /// Admin: Lấy danh sách feedback với pagination, filter, search, sort
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> ListForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? customerId = null,
        [FromQuery] int? bookingId = null,
        [FromQuery] int? orderId = null,
        [FromQuery] int? partId = null,
        [FromQuery] int? technicianId = null,
        [FromQuery] int? minRating = null,
        [FromQuery] int? maxRating = null,
        [FromQuery] bool? isAnonymous = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc")
    {
        try
        {
            if (page < 1) return BadRequest(new { success = false, message = "Page phải lớn hơn 0" });
            if (pageSize < 1 || pageSize > 100) return BadRequest(new { success = false, message = "Page size phải từ 1 đến 100" });

            var q = _db.Feedbacks
                .AsNoTracking()
                .Include(f => f.Customer)
                    .ThenInclude(c => c.User)
                .Include(f => f.Part)
                .Include(f => f.Technician)
                    .ThenInclude(t => t!.User)
                // .Include(f => f.Booking) // BookingID column does not exist in database
                .Include(f => f.Order)
                .AsQueryable();

            // Apply filters
            if (customerId.HasValue)
                q = q.Where(f => f.CustomerId == customerId.Value);

            // BookingId filter removed - BookingID column does not exist in database
            // if (bookingId.HasValue)
            //     q = q.Where(f => f.BookingId == bookingId.Value);

            if (orderId.HasValue)
                q = q.Where(f => f.OrderId == orderId.Value);

            if (partId.HasValue)
                q = q.Where(f => f.PartId == partId.Value);

            if (technicianId.HasValue)
                q = q.Where(f => f.TechnicianId == technicianId.Value);

            if (minRating.HasValue)
                q = q.Where(f => f.Rating >= minRating.Value);

            if (maxRating.HasValue)
                q = q.Where(f => f.Rating <= maxRating.Value);

            if (isAnonymous.HasValue)
                q = q.Where(f => f.IsAnonymous == isAnonymous.Value);

            if (from.HasValue)
                q = q.Where(f => f.CreatedAt >= from.Value);

            if (to.HasValue)
                q = q.Where(f => f.CreatedAt <= to.Value);

            // Search term - search by customer name, part name, technician name, comment
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                q = q.Where(f =>
                    (f.Customer != null && f.Customer.User != null && f.Customer.User.FullName != null && f.Customer.User.FullName.ToLower().Contains(search)) ||
                    (f.Part != null && f.Part.PartName != null && f.Part.PartName.ToLower().Contains(search)) ||
                    (f.Technician != null && f.Technician.User != null && f.Technician.User.FullName != null && f.Technician.User.FullName.ToLower().Contains(search)) ||
                    (f.Comment != null && f.Comment.ToLower().Contains(search))
                );
            }

            // Get total count before pagination
            var totalCount = await q.CountAsync();

            // Apply sorting
            var isDescending = sortOrder?.ToLowerInvariant() == "desc";
            switch (sortBy?.ToLowerInvariant())
            {
                case "rating":
                    q = isDescending ? q.OrderByDescending(f => f.Rating) : q.OrderBy(f => f.Rating);
                    break;
                case "createdat":
                default:
                    q = isDescending ? q.OrderByDescending(f => f.CreatedAt) : q.OrderBy(f => f.CreatedAt);
                    break;
            }

            // Apply pagination
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    feedbackId = f.FeedbackId,
                    customerId = f.CustomerId,
                    customerName = f.Customer != null && f.Customer.User != null ? f.Customer.User.FullName : null,
                    customerEmail = f.Customer != null && f.Customer.User != null ? f.Customer.User.Email : null,
                    bookingId = (int?)null, // BookingID column does not exist in database
                    orderId = f.OrderId,
                    partId = f.PartId,
                    partName = f.Part != null ? f.Part.PartName : null,
                    technicianId = f.TechnicianId,
                    technicianName = f.Technician != null && f.Technician.User != null ? f.Technician.User.FullName : null,
                    rating = f.Rating,
                    comment = f.Comment,
                    isAnonymous = f.IsAnonymous,
                    createdAt = f.CreatedAt
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(new { success = true, data = items, pagination });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách feedback: {ex.Message}" });
        }
    }

    /// <summary>
    /// Admin: Lấy thống kê feedback
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var allFeedbacks = await _db.Feedbacks.AsNoTracking().ToListAsync();

            var total = allFeedbacks.Count;
            var avgRating = total > 0 ? Math.Round(allFeedbacks.Average(f => (double)f.Rating), 2) : 0;

            var byRating = allFeedbacks
                .GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            var anonymousCount = allFeedbacks.Count(f => f.IsAnonymous);
            var nonAnonymousCount = total - anonymousCount;

            var byType = new
            {
                part = allFeedbacks.Count(f => f.PartId.HasValue && !f.TechnicianId.HasValue),
                technician = allFeedbacks.Count(f => f.TechnicianId.HasValue && !f.PartId.HasValue),
                both = allFeedbacks.Count(f => f.PartId.HasValue && f.TechnicianId.HasValue),
                general = allFeedbacks.Count(f => !f.PartId.HasValue && !f.TechnicianId.HasValue)
            };

            var bySource = new
            {
                booking = 0, // BookingID column does not exist in database
                order = allFeedbacks.Count(f => f.OrderId.HasValue),
                public_ = allFeedbacks.Count(f => !f.OrderId.HasValue) // Removed BookingId check since column doesn't exist
            };

            var recentCount = allFeedbacks.Count(f => f.CreatedAt >= DateTime.UtcNow.AddDays(-7));
            var thisMonthCount = allFeedbacks.Count(f => f.CreatedAt >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1));

            return Ok(new
            {
                success = true,
                data = new
                {
                    total,
                    averageRating = avgRating,
                    byRating = new Dictionary<int, int>
                    {
                        { 1, byRating.ContainsKey(1) ? byRating[1] : 0 },
                        { 2, byRating.ContainsKey(2) ? byRating[2] : 0 },
                        { 3, byRating.ContainsKey(3) ? byRating[3] : 0 },
                        { 4, byRating.ContainsKey(4) ? byRating[4] : 0 },
                        { 5, byRating.ContainsKey(5) ? byRating[5] : 0 }
                    },
                    anonymous = anonymousCount,
                    nonAnonymous = nonAnonymousCount,
                    byType,
                    bySource,
                    recent = recentCount,
                    thisMonth = thisMonthCount
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thống kê feedback: {ex.Message}" });
        }
    }
}
