using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/service-templates")]
public class ServiceChecklistController : ControllerBase
{
    private readonly IServiceChecklistRepository _repo;
    public ServiceChecklistController(IServiceChecklistRepository repo) { _repo = repo; }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Get([FromQuery] int serviceId, [FromQuery] bool activeOnly = false)
    {
        if (serviceId <= 0) return BadRequest("serviceId is required");
        var list = await _repo.GetTemplatesAsync(serviceId, activeOnly);
        return Ok(new { items = list, total = list.Count });
    }

    [HttpGet("all")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync();
        return Ok(new { items = list, total = list.Count });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] int? serviceId)
    {
        var list = await _repo.GetActiveAsync(serviceId);
        return Ok(new { items = list, total = list.Count });
    }

    public class TemplateCreateRequest
    {
        public int ServiceId { get; set; }
        public string? TemplateName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<TemplateItemDto> Items { get; set; } = new();
    }
    public class TemplateItemDto { public int ItemId { get; set; } public int PartId { get; set; } public decimal? DefaultQuantity { get; set; } }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] TemplateCreateRequest req)
    {
        if (req == null || req.ServiceId <= 0 || string.IsNullOrWhiteSpace(req.TemplateName)) return BadRequest("Invalid request");
        var template = new ServiceChecklistTemplate
        {
            ServiceID = req.ServiceId,
            TemplateName = req.TemplateName.Trim(),
            Description = req.Description,
            IsActive = req.IsActive
        };
        var items = (req.Items ?? new List<TemplateItemDto>())
            .Select(i => new ServiceChecklistTemplateItem { PartID = i.PartId })
            .ToList();
        var id = await _repo.CreateTemplateAsync(template, items);
        if (req.IsActive) await _repo.SetActiveAsync(id, true);
        return StatusCode(201, new { templateId = id });
    }

    public class TemplateUpdateRequest { public string? TemplateName { get; set; } public string? Description { get; set; } }

    [HttpPut("{templateId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int templateId, [FromBody] TemplateUpdateRequest req)
    {
        var tmpl = await _repo.GetByIdAsync(templateId);
        if (tmpl == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req?.TemplateName)) tmpl.TemplateName = req.TemplateName.Trim();
        if (req != null) tmpl.Description = req.Description;
        tmpl.UpdatedAt = System.DateTime.UtcNow;
        await _repo.UpdateTemplateAsync(tmpl);
        return Ok(new { templateId });
    }

    public class UpsertItemsRequest { public List<TemplateItemDto> Items { get; set; } = new(); }

    [HttpPut("{templateId:int}/items")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpsertItems(int templateId, [FromBody] UpsertItemsRequest req)
    {
        var tmpl = await _repo.GetByIdAsync(templateId);
        if (tmpl == null) return NotFound();
        var items = (req?.Items ?? new List<TemplateItemDto>())
            .Select(i => new ServiceChecklistTemplateItem { ItemID = i.ItemId, PartID = i.PartId, TemplateID = templateId })
            .ToList();
        await _repo.UpsertItemsAsync(templateId, items);
        return Ok(new { updated = true });
    }

    public class ActivateRequest { public bool IsActive { get; set; } }

    [HttpPut("{templateId:int}/activate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Activate(int templateId, [FromBody] ActivateRequest req)
    {
        await _repo.SetActiveAsync(templateId, req?.IsActive ?? true);
        return Ok(new { templateId, isActive = req?.IsActive ?? true });
    }

    [HttpDelete("{templateId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int templateId)
    {
        await _repo.DeleteTemplateAsync(templateId);
        return NoContent();
    }

    [HttpGet("{templateId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetById(int templateId)
    {
        var tmpl = await _repo.GetByIdAsync(templateId);
        if (tmpl == null) return NotFound();
        return Ok(tmpl);
    }

    // Public: lấy danh sách Part của một template
    [HttpGet("{templateId:int}/items")] 
    public async Task<IActionResult> GetItems(int templateId)
    {
        var tmpl = await _repo.GetByIdAsync(templateId);
        if (tmpl == null) return NotFound();
        var items = await _repo.GetItemsByTemplateAsync(templateId);
        
        var response = new
        {
            templateId,
            templateName = tmpl.TemplateName,
            items = items.Select(i => new
            {
                itemId = i.ItemID,
                partId = i.PartID,
                partName = i.Part?.PartName,
                partNumber = i.Part?.PartNumber,
                brand = i.Part?.Brand,
                price = i.Part?.Price,
                createdAt = i.CreatedAt
            })
        };
        
        return Ok(response);
    }

    // Thêm part vào template
    [HttpPost("{templateId:int}/parts/{partId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddPart(int templateId, int partId)
    {
        try
        {
            await _repo.AddPartToTemplateAsync(templateId, partId);
            return Ok(new { message = "Đã thêm part vào template thành công", templateId, partId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Xóa part khỏi template
    [HttpDelete("{templateId:int}/parts/{partId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RemovePart(int templateId, int partId)
    {
        try
        {
            await _repo.RemovePartFromTemplateAsync(templateId, partId);
            return Ok(new { message = "Đã xóa part khỏi template thành công", templateId, partId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Thêm nhiều part vào template cùng lúc
    [HttpPost("{templateId:int}/parts/batch")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AddPartsBatch(int templateId, [FromBody] BatchPartsRequest request)
    {
        try
        {
            if (request?.PartIds == null || !request.PartIds.Any())
                return BadRequest(new { message = "Danh sách PartIds không được rỗng" });

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var partId in request.PartIds)
            {
                try
                {
                    await _repo.AddPartToTemplateAsync(templateId, partId);
                    results.Add(new { partId, success = true, message = "Thành công" });
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"PartId {partId}: {ex.Message}");
                    results.Add(new { partId, success = false, message = ex.Message });
                }
            }

            return Ok(new 
            { 
                message = $"Đã xử lý {request.PartIds.Count} part", 
                templateId, 
                results,
                errors = errors.Any() ? errors : null
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Xóa nhiều part khỏi template cùng lúc
    [HttpDelete("{templateId:int}/parts/batch")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RemovePartsBatch(int templateId, [FromBody] BatchPartsRequest request)
    {
        try
        {
            if (request?.PartIds == null || !request.PartIds.Any())
                return BadRequest(new { message = "Danh sách PartIds không được rỗng" });

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var partId in request.PartIds)
            {
                try
                {
                    await _repo.RemovePartFromTemplateAsync(templateId, partId);
                    results.Add(new { partId, success = true, message = "Thành công" });
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"PartId {partId}: {ex.Message}");
                    results.Add(new { partId, success = false, message = ex.Message });
                }
            }

            return Ok(new 
            { 
                message = $"Đã xử lý {request.PartIds.Count} part", 
                templateId, 
                results,
                errors = errors.Any() ? errors : null
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class BatchPartsRequest
    {
        public List<int> PartIds { get; set; } = new();
    }
}
