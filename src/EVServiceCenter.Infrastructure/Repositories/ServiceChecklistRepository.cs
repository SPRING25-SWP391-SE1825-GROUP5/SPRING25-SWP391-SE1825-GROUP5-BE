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
            .Include(i => i.Part)
            .Where(i => i.TemplateID == templateId)
            .OrderBy(i => i.ItemID)
            .ToListAsync();

    public async Task AddPartToTemplateAsync(int templateId, int partId)
    {
        // Kiểm tra template có tồn tại không
        var template = await _db.ServiceChecklistTemplates.FirstOrDefaultAsync(t => t.TemplateID == templateId);
        if (template == null) throw new ArgumentException("Template không tồn tại");

        // Kiểm tra part có tồn tại không
        var part = await _db.Parts.FirstOrDefaultAsync(p => p.PartId == partId);
        if (part == null) throw new ArgumentException("Part không tồn tại");

        // Kiểm tra part đã có trong template chưa
        var existing = await _db.ServiceChecklistTemplateItems
            .FirstOrDefaultAsync(i => i.TemplateID == templateId && i.PartID == partId);
        if (existing != null) throw new ArgumentException("Part đã có trong template");

        // Thêm part vào template
        var item = new ServiceChecklistTemplateItem
        {
            TemplateID = templateId,
            PartID = partId,
            CreatedAt = DateTime.UtcNow
        };
        await _db.ServiceChecklistTemplateItems.AddAsync(item);
        await _db.SaveChangesAsync();
    }

    public async Task RemovePartFromTemplateAsync(int templateId, int partId)
    {
        var item = await _db.ServiceChecklistTemplateItems
            .FirstOrDefaultAsync(i => i.TemplateID == templateId && i.PartID == partId);
        if (item == null) throw new ArgumentException("Part không có trong template");

        _db.ServiceChecklistTemplateItems.Remove(item);
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
                found.PartID = incoming.PartID;
                // nếu sau này có DefaultQuantity thì set tại đây
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
}
