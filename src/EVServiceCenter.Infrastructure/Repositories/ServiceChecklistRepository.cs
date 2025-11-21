using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class ServiceChecklistRepository : IServiceChecklistRepository
{
    private readonly EVDbContext _db;
    public ServiceChecklistRepository(EVDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ServiceChecklistTemplate>> GetTemplatesAsync(int serviceId, bool activeOnly)
    {
        var q = _db.ServiceChecklistTemplates.AsNoTracking().Where(t => t.ServiceID == serviceId);
        if (activeOnly) q = q.Where(t => t.IsActive);
        return await q.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<ServiceChecklistTemplate>> GetAllAsync()
        => await _db.ServiceChecklistTemplates.AsNoTracking()
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<ServiceChecklistTemplate>> GetActiveAsync(int? serviceId)
    {
        var q = _db.ServiceChecklistTemplates.AsNoTracking().Where(t => t.IsActive);
        if (serviceId.HasValue && serviceId.Value > 0)
            q = q.Where(t => t.ServiceID == serviceId.Value);
        return await q.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<ServiceChecklistTemplateItem>> GetItemsByTemplateAsync(int templateId)
        => await _db.ServiceChecklistTemplateItems.AsNoTracking()
            .Include(i => i.Category)
            .Where(i => i.TemplateID == templateId)
            .OrderBy(i => i.ItemID)
            .ToListAsync();

    public async Task AddCategoryToTemplateAsync(int templateId, int categoryId)
    {
        var template = await _db.ServiceChecklistTemplates.FirstOrDefaultAsync(t => t.TemplateID == templateId);
        if (template == null) throw new ArgumentException("Template không tồn tại");

        var category = await _db.PartCategories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category == null) throw new ArgumentException("Category không tồn tại");

        var existing = await _db.ServiceChecklistTemplateItems
            .FirstOrDefaultAsync(i => i.TemplateID == templateId && i.CategoryId == categoryId);
        if (existing != null) throw new ArgumentException("Category đã có trong template");

        var item = new ServiceChecklistTemplateItem
        {
            TemplateID = templateId,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };
        await _db.ServiceChecklistTemplateItems.AddAsync(item);
        await _db.SaveChangesAsync();
    }

    public Task<ServiceChecklistTemplate?> GetByIdAsync(int templateId)
        => _db.ServiceChecklistTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.TemplateID == templateId);

    public async Task<int> CreateTemplateAsync(ServiceChecklistTemplate template, IEnumerable<ServiceChecklistTemplateItem> items)
    {
        await _db.ServiceChecklistTemplates.AddAsync(template);
        await _db.SaveChangesAsync();
        var tid = template.TemplateID;
        foreach (var i in items)
        {
            i.TemplateID = tid;
            await _db.ServiceChecklistTemplateItems.AddAsync(i);
        }
        await _db.SaveChangesAsync();
        return tid;
    }

    public async Task UpdateTemplateAsync(ServiceChecklistTemplate template)
    {
        _db.ServiceChecklistTemplates.Update(template);
        await _db.SaveChangesAsync();
    }

    public async Task UpsertItemsAsync(int templateId, IEnumerable<ServiceChecklistTemplateItem> items)
    {
        var existing = await _db.ServiceChecklistTemplateItems.Where(i => i.TemplateID == templateId).ToListAsync();
        var byKey = existing.ToDictionary(x => x.ItemID);

        // update/insert
        foreach (var incoming in items)
        {
            if (incoming.ItemID != 0 && byKey.TryGetValue(incoming.ItemID, out var found))
            {
                found.CategoryId = incoming.CategoryId;
            }
            else
            {
                incoming.TemplateID = templateId;
                _db.ServiceChecklistTemplateItems.Add(incoming);
            }
        }

        // delete missing
        var incomingIds = items.Where(i => i.ItemID != 0).Select(i => i.ItemID).ToHashSet();
        var toDelete = existing.Where(e => !incomingIds.Contains(e.ItemID)).ToList();
        _db.ServiceChecklistTemplateItems.RemoveRange(toDelete);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteItemsBatchAsync(int templateId, IEnumerable<int> categoryIds)
    {
        var categoryIdList = categoryIds.ToList();
        if (!categoryIdList.Any())
            return;

        // Xóa tất cả items có TemplateID và CategoryId khớp
        var itemsToDelete = await _db.ServiceChecklistTemplateItems
            .Where(i => i.TemplateID == templateId && i.CategoryId.HasValue && categoryIdList.Contains(i.CategoryId.Value))
            .ToListAsync();

        if (itemsToDelete.Any())
        {
            _db.ServiceChecklistTemplateItems.RemoveRange(itemsToDelete);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SetActiveAsync(int templateId, bool isActive)
    {
        var tmpl = await _db.ServiceChecklistTemplates.FirstOrDefaultAsync(t => t.TemplateID == templateId);
        if (tmpl == null) return;
        if (isActive)
        {
            var sameService = await _db.ServiceChecklistTemplates.Where(t => t.ServiceID == tmpl.ServiceID).ToListAsync();
            foreach (var t in sameService) t.IsActive = t.TemplateID == templateId;
        }
        else
        {
            tmpl.IsActive = false;
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTemplateAsync(int templateId)
    {
        var tmpl = await _db.ServiceChecklistTemplates.FirstOrDefaultAsync(t => t.TemplateID == templateId);
        if (tmpl == null) return;
        var items = _db.ServiceChecklistTemplateItems.Where(i => i.TemplateID == templateId);
        _db.ServiceChecklistTemplateItems.RemoveRange(items);
        _db.ServiceChecklistTemplates.Remove(tmpl);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ServiceChecklistTemplate>> GetRecommendedTemplatesAsync(
        int currentKm,
        DateTime? lastMaintenanceDate,
        int? categoryId = null)
    {
        var query = _db.ServiceChecklistTemplates
            .AsNoTracking()
            .Include(t => t.Service)
            .Where(t => t.IsActive);

        // Filter by category if provided
        if (categoryId.HasValue)
        {
            query = query.Where(t => t.Service != null && t.Service.CategoryId == categoryId.Value);
        }

        var templates = await query.ToListAsync();

        // Calculate recommendation scores
        var scoredTemplates = templates.Select(template => new
        {
            Template = template,
            Score = CalculateRecommendationScore(template, currentKm, lastMaintenanceDate)
        })
        .OrderByDescending(x => x.Score)
        .Select(x => x.Template)
        .ToList();

        return scoredTemplates.AsReadOnly();
    }

    private int CalculateRecommendationScore(ServiceChecklistTemplate template, int currentKm, DateTime? lastMaintenanceDate)
    {
        int score = 0;
        bool hasKmConditions = template.MinKm.HasValue;
        bool hasDateConditions = template.MaxDate.HasValue;
        bool hasAnyConditions = hasKmConditions || hasDateConditions;

        // Base score for all active templates (ensures we always have some recommendations)
        score += 10;

        // 1. Kiểm tra điều kiện MinKm trước
        if (template.MinKm.HasValue)
        {
            if (currentKm >= template.MinKm.Value)
            {
                score += 80; // Đạt ngưỡng km tối thiểu
            }
            else
            {
                // Chưa đạt ngưỡng km tối thiểu - vẫn có điểm nhưng thấp hơn
                var diff = template.MinKm.Value - currentKm;
                if (diff <= 2000) // Trong vòng 2000km
                {
                    score += 40; // Gần đến ngưỡng
                }
                else
                {
                    score += 20; // Còn xa ngưỡng
                }
            }
        }
        else
        {
            // Không có điều kiện km - template tổng quát
            score += 50;
        }

        // 2. Kiểm tra điều kiện MaxDate (ngày tối đa để hiển thị template)
        if (lastMaintenanceDate.HasValue && template.MaxDate.HasValue)
        {
            var daysSinceLastMaintenance = (DateTime.UtcNow - lastMaintenanceDate.Value).Days;
            var maxDate = template.MaxDate.Value;

            if (daysSinceLastMaintenance <= maxDate)
            {
                // Ngày bảo dưỡng cuối còn trong phạm vi cho phép
                score += 60;
            }
            else
            {
                // Ngày bảo dưỡng cuối quá xa - vẫn hiển thị nhưng điểm thấp
                var overdueDays = daysSinceLastMaintenance - maxDate;
                if (overdueDays <= 30) // Trễ dưới 30 ngày
                {
                    score += 30; // Vẫn có thể chấp nhận
                }
                else
                {
                    score += 10; // Quá xa, cần xem xét lại
                }
            }
        }
        else if (template.MaxDate.HasValue)
        {
            // Có MaxDate nhưng không có ngày bảo dưỡng cuối
            score += 30; // Điểm trung bình
        }

        // 3. Không có IntervalDays trong database - bỏ qua phần này

        // 4. Bonus cho templates có điều kiện đầy đủ
        if (hasKmConditions && hasDateConditions)
        {
            score += 20; // Bonus cho template comprehensive
        }

        // 5. Đảm bảo điểm tối thiểu cho templates không có điều kiện cụ thể
        if (!hasAnyConditions)
        {
            score = Math.Max(score, 30); // Minimum score for general templates
        }

        return score;
    }
}
