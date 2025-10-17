using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly EVServiceCenter.Infrastructure.Configurations.EVDbContext _db;
    private readonly ICustomerRepository _customerRepository;
    public FeedbackController(EVServiceCenter.Infrastructure.Configurations.EVDbContext db, ICustomerRepository customerRepository) { _db = db; _customerRepository = customerRepository; }

    // Generic POST /api/Feedback endpoint removed: use specific endpoints below

    // Split endpoints
    [HttpPost("orders/{orderId:int}/parts/{partId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> CreateForOrderPart(int orderId, int partId, [FromBody] CreateOrderPartFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        // Validate order and require completed status before allowing feedback
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null) return BadRequest(new { success = false, message = $"orderId {orderId} không tồn tại" });
        var completedStatuses = new[] { "COMPLETED", "DONE", "FINISHED" };
        if (string.IsNullOrWhiteSpace(order.Status) || !completedStatuses.Contains(order.Status.ToUpper()))
        {
            return BadRequest(new { success = false, message = "Chỉ được đánh giá sau khi đơn hàng đã hoàn thành" });
        }
        var partExists = await _db.Parts.AsNoTracking().AnyAsync(p => p.PartId == partId);
        if (!partExists) return BadRequest(new { success = false, message = $"partId {partId} không tồn tại" });
        // Ensure part belongs to the order
        var partInOrder = await _db.OrderItems.AsNoTracking().AnyAsync(oi => oi.OrderId == orderId && oi.PartId == partId);
        if (!partInOrder) return BadRequest(new { success = false, message = "Phụ tùng không thuộc đơn hàng này" });
        // Customer check
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null) return BadRequest(new { success = false, message = $"customerId {request.CustomerId} không tồn tại" });

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = orderId,        // Từ path parameter
            WorkOrderId = null,
            PartId = partId,          // Từ path parameter
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

    [HttpPost("workorders/{workOrderId:int}/parts/{partId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> CreateForWorkOrderPart(int workOrderId, int partId, [FromBody] CreateWorkOrderPartFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        // Validate work order and require completed status before allowing feedback
        var workOrder = await _db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId);
        if (workOrder == null) return BadRequest(new { success = false, message = $"workOrderId {workOrderId} không tồn tại" });
        var woCompletedStatuses = new[] { "COMPLETED", "DONE", "FINISHED" };
        if (string.IsNullOrWhiteSpace(workOrder.Status) || !woCompletedStatuses.Contains(workOrder.Status.ToUpper()))
        {
            return BadRequest(new { success = false, message = "Chỉ được đánh giá sau khi work order đã hoàn thành" });
        }
        var partExists = await _db.Parts.AsNoTracking().AnyAsync(p => p.PartId == partId);
        if (!partExists) return BadRequest(new { success = false, message = $"partId {partId} không tồn tại" });
        // Ensure part belongs to the work order
        var partInWorkOrder = await _db.WorkOrderParts.AsNoTracking().AnyAsync(wop => wop.WorkOrderId == workOrderId && wop.PartId == partId);
        if (!partInWorkOrder) return BadRequest(new { success = false, message = "Phụ tùng không thuộc work order này" });
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null) return BadRequest(new { success = false, message = $"customerId {request.CustomerId} không tồn tại" });

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = null,
            WorkOrderId = workOrderId,    // Từ path parameter
            PartId = partId,              // Từ path parameter
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

    [HttpPost("workorders/{workOrderId:int}/technicians/{technicianId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> CreateForWorkOrderTechnician(int workOrderId, int technicianId, [FromBody] CreateWorkOrderTechnicianFeedbackRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        // Validate work order and require completed status before allowing feedback
        var wo = await _db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId);
        if (wo == null) return BadRequest(new { success = false, message = $"workOrderId {workOrderId} không tồn tại" });
        var completedWoStatuses = new[] { "COMPLETED", "DONE", "FINISHED" };
        if (string.IsNullOrWhiteSpace(wo.Status) || !completedWoStatuses.Contains(wo.Status.ToUpper()))
        {
            return BadRequest(new { success = false, message = "Chỉ được đánh giá sau khi work order đã hoàn thành" });
        }
        var techExists = await _db.Technicians.AsNoTracking().AnyAsync(t => t.TechnicianId == technicianId);
        if (!techExists) return BadRequest(new { success = false, message = $"technicianId {technicianId} không tồn tại" });
        // Ensure technician matches the work order
        if (wo.TechnicianId != technicianId)
        {
            return BadRequest(new { success = false, message = "Kỹ thuật viên không phụ trách work order này" });
        }
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null) return BadRequest(new { success = false, message = $"customerId {request.CustomerId} không tồn tại" });

        var fb = new Feedback
        {
            CustomerId = request.CustomerId,
            OrderId = null,
            WorkOrderId = workOrderId,        // Từ path parameter
            PartId = null,
            TechnicianId = technicianId,      // Từ path parameter
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
        // Ownership check: only owner or Admin
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
            .Select(x => new { x.FeedbackId, x.CustomerId, x.PartId, x.TechnicianId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.OrderId, x.WorkOrderId })
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

    [HttpGet("workorders/{workOrderId:int}")]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> ListByWorkOrder(int workOrderId)
    {
        var data = await _db.Feedbacks.AsNoTracking().Where(x => x.WorkOrderId == workOrderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.FeedbackId, x.Rating, x.Comment, x.IsAnonymous, x.CreatedAt, x.PartId, x.TechnicianId })
            .ToListAsync();
        return Ok(new { success = true, data });
    }
}


