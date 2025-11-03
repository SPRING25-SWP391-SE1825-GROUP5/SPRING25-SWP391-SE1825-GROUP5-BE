using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/service-templates")]
public class ServiceChecklistController : ControllerBase
{
    private readonly IServiceChecklistRepository _repo;

    public ServiceChecklistController(IServiceChecklistRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("templates/{serviceId}")]
    public async Task<IActionResult> GetTemplates(int serviceId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var templates = await _repo.GetTemplatesAsync(serviceId, activeOnly);
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy danh sách template", error = ex.Message });
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllTemplates()
    {
        try
        {
            var templates = await _repo.GetAllAsync();
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy tất cả template", error = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTemplates([FromQuery] int? serviceId = null)
    {
        try
        {
            var templates = await _repo.GetActiveAsync(serviceId);
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy template active", error = ex.Message });
        }
    }

    [HttpGet("{templateId}")]
    public async Task<IActionResult> GetTemplate(int templateId)
    {
        try
        {
            var template = await _repo.GetByIdAsync(templateId);
            if (template == null)
                return NotFound(new { success = false, message = "Không tìm thấy template" });

            return Ok(new { success = true, data = template });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy template", error = ex.Message });
        }
    }

    [HttpGet("{templateId}/items")]
    public async Task<IActionResult> GetTemplateItems(int templateId)
    {
        try
        {
            var items = await _repo.GetItemsByTemplateAsync(templateId);
            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lấy danh sách items", error = ex.Message });
        }
    }

    [HttpPost("{templateId}/parts/{partId}")]
    public async Task<IActionResult> AddPartToTemplate(int templateId, int partId)
    {
        try
        {
            await _repo.AddPartToTemplateAsync(templateId, partId);
            return Ok(new { success = true, message = "Thêm part vào template thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi thêm part vào template", error = ex.Message });
        }
    }

    [HttpDelete("{templateId}/parts/{partId}")]
    public async Task<IActionResult> RemovePartFromTemplate(int templateId, int partId)
    {
        try
        {
            await _repo.RemovePartFromTemplateAsync(templateId, partId);
            return Ok(new { success = true, message = "Xóa part khỏi template thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi xóa part khỏi template", error = ex.Message });
        }
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            if (request.Template == null || request.Items == null)
                return BadRequest(new { success = false, message = "Dữ liệu template và items không được để trống" });

            var templateId = await _repo.CreateTemplateAsync(request.Template, request.Items);
            return Ok(new { success = true, data = new { templateId }, message = "Tạo template thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo template", error = ex.Message });
        }
    }

    [HttpPut("{templateId}")]
    public async Task<IActionResult> UpdateTemplate(int templateId, [FromBody] ServiceChecklistTemplate template)
    {
        try
        {
            if (template.TemplateID != templateId)
                return BadRequest(new { success = false, message = "ID template không khớp" });

            await _repo.UpdateTemplateAsync(template);
            return Ok(new { success = true, message = "Cập nhật template thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật template", error = ex.Message });
        }
    }

    [HttpPut("{templateId}/items")]
    public async Task<IActionResult> UpsertItems(int templateId, [FromBody] IEnumerable<ServiceChecklistTemplateItem> items)
    {
        try
        {
            await _repo.UpsertItemsAsync(templateId, items);
            return Ok(new { success = true, message = "Cập nhật items thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật items", error = ex.Message });
        }
    }

    [HttpPatch("{templateId}/active")]
    public async Task<IActionResult> SetActive(int templateId, [FromBody] SetActiveRequest request)
    {
        try
        {
            await _repo.SetActiveAsync(templateId, request.IsActive);
            return Ok(new { success = true, message = "Cập nhật trạng thái template thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi cập nhật trạng thái template", error = ex.Message });
        }
    }

    [HttpDelete("{templateId}")]
    public async Task<IActionResult> DeleteTemplate(int templateId)
    {
        try
        {
            await _repo.DeleteTemplateAsync(templateId);
            return Ok(new { success = true, message = "Xóa template thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi xóa template", error = ex.Message });
        }
    }

    [HttpGet("recommend")]
    public async Task<IActionResult> GetRecommendedServices(
        [FromQuery] int currentKm,
        [FromQuery] DateTime? lastMaintenanceDate = null,
        [FromQuery] int? categoryId = null)
    {
        if (currentKm < 0)
            return BadRequest(new { message = "Số km hiện tại phải >= 0" });

        try
        {
            var recommendedTemplates = await _repo.GetRecommendedTemplatesAsync(
                currentKm, 
                lastMaintenanceDate, 
                categoryId);

            var response = recommendedTemplates.Select((template, index) => new
            {
                templateId = template.TemplateID,
                serviceId = template.ServiceID,
                templateName = template.TemplateName,
                description = template.Description,
                serviceName = template.Service?.ServiceName,
                categoryId = template.Service?.CategoryId,
                categoryName = template.Service?.Category?.CategoryName,
                minKm = template.MinKm,
                maxDate = template.MaxDate,
                maxOverdueDays = template.MaxOverdueDays,
                createdAt = template.CreatedAt,
                updatedAt = template.UpdatedAt,
                recommendationRank = index + 1,
                recommendationReason = GetRecommendationReason(template, currentKm, lastMaintenanceDate),
                warnings = GetWarnings(template, currentKm, lastMaintenanceDate)
            }).ToList();

            return Ok(new 
            { 
                success = true,
                data = response,
                total = response.Count,
                message = $"Tìm thấy {response.Count} dịch vụ phù hợp"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false,
                message = "Lỗi khi tìm kiếm dịch vụ phù hợp",
                error = ex.Message
            });
        }
    }

    private string GetRecommendationReason(ServiceChecklistTemplate template, int currentKm, DateTime? lastMaintenanceDate)
    {
        var reasons = new List<string>();

        // Km-based reasons
        if (template.MinKm.HasValue)
        {
            if (currentKm >= template.MinKm.Value)
            {
                reasons.Add($"Số km hiện tại ({currentKm:N0}) đã đạt ngưỡng bảo dưỡng ({template.MinKm:N0} km)");
            }
            else
            {
                var diff = template.MinKm.Value - currentKm;
                reasons.Add($"Số km hiện tại ({currentKm:N0}) gần đến ngưỡng bảo dưỡng ({template.MinKm:N0} km) - còn {diff:N0} km");
            }
        }
        else
        {
            reasons.Add("Dịch vụ bảo dưỡng tổng quát phù hợp với mọi số km");
        }

        // Date-based reasons (chỉ dùng MaxDate vì database không có IntervalDays)
        if (lastMaintenanceDate.HasValue && template.MaxDate.HasValue)
        {
            var daysSinceLastMaintenance = (DateTime.UtcNow - lastMaintenanceDate.Value).Days;
            var maxDate = template.MaxDate.Value;

            if (daysSinceLastMaintenance <= maxDate)
            {
                reasons.Add($"Ngày bảo dưỡng cuối ({daysSinceLastMaintenance} ngày trước) trong phạm vi cho phép ({maxDate} ngày)");
            }
            else
            {
                var overdueDays = daysSinceLastMaintenance - maxDate;
                if (template.MaxOverdueDays.HasValue && overdueDays <= template.MaxOverdueDays.Value)
                {
                    reasons.Add($"Ngày bảo dưỡng cuối ({daysSinceLastMaintenance} ngày trước) đã quá {overdueDays} ngày so với ngưỡng cho phép");
                }
                else
                {
                    reasons.Add($"Ngày bảo dưỡng cuối ({daysSinceLastMaintenance} ngày trước) đã quá xa - cần xem xét lại");
                }
            }
        }
        else if (lastMaintenanceDate.HasValue)
        {
            var daysSinceLastMaintenance = (DateTime.UtcNow - lastMaintenanceDate.Value).Days;
            reasons.Add($"Lần bảo dưỡng cuối cách đây {daysSinceLastMaintenance} ngày");
        }
        else
        {
            reasons.Add("Không có thông tin về lần bảo dưỡng cuối");
        }

        return string.Join("; ", reasons);
    }

    private List<string> GetWarnings(ServiceChecklistTemplate template, int currentKm, DateTime? lastMaintenanceDate)
    {
        var warnings = new List<string>();

        // Warning cho MaxOverdueDays - chỉ dùng MaxDate, không dùng IntervalDays
        if (lastMaintenanceDate.HasValue && template.MaxDate.HasValue && template.MaxOverdueDays.HasValue)
        {
            var daysSinceLastMaintenance = (DateTime.UtcNow - lastMaintenanceDate.Value).Days;
            var maxDate = template.MaxDate.Value;
            var maxOverdueDays = template.MaxOverdueDays.Value;

            if (daysSinceLastMaintenance > maxDate)
            {
                var overdueDays = daysSinceLastMaintenance - maxDate;
                if (overdueDays <= maxOverdueDays)
                {
                    warnings.Add($"⚠️ Dịch vụ này đã quá hạn {overdueDays} ngày so với ngưỡng cho phép ({maxDate} ngày, cho phép trễ tối đa {maxOverdueDays} ngày). Vui lòng xem xét lại tình trạng xe hiện tại.");
                }
                else
                {
                    warnings.Add($"🚨 Dịch vụ này đã quá hạn {overdueDays} ngày (vượt quá giới hạn {maxOverdueDays} ngày). Có thể không phù hợp với tình trạng xe hiện tại.");
                }
            }
        }

        // Warning cho MaxDate
        if (lastMaintenanceDate.HasValue && template.MaxDate.HasValue)
        {
            var daysSinceLastMaintenance = (DateTime.UtcNow - lastMaintenanceDate.Value).Days;
            var maxDate = template.MaxDate.Value;

            if (daysSinceLastMaintenance > maxDate)
            {
                var overdueDays = daysSinceLastMaintenance - maxDate;
                warnings.Add($"⚠️ Lần bảo dưỡng cuối đã quá {overdueDays} ngày so với ngưỡng cho phép ({maxDate} ngày). Dịch vụ này có thể không phù hợp.");
            }
        }

        // Warning cho MinKm
        if (template.MinKm.HasValue && currentKm < template.MinKm.Value)
        {
            var diff = template.MinKm.Value - currentKm;
            warnings.Add($"ℹ️ Xe chưa đạt ngưỡng km tối thiểu ({template.MinKm:N0} km). Còn thiếu {diff:N0} km. Dịch vụ này có thể chưa cần thiết.");
        }

        return warnings;
    }
}

public class CreateTemplateRequest
{
    public ServiceChecklistTemplate Template { get; set; } = null!;
    public IEnumerable<ServiceChecklistTemplateItem> Items { get; set; } = null!;
}

public class SetActiveRequest
{
    public bool IsActive { get; set; }
}