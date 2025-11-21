using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IServiceChecklistRepository
{
    Task<IReadOnlyList<ServiceChecklistTemplate>> GetTemplatesAsync(int serviceId, bool activeOnly);
    Task<IReadOnlyList<ServiceChecklistTemplate>> GetAllAsync();
    Task<IReadOnlyList<ServiceChecklistTemplate>> GetActiveAsync(int? serviceId);
    Task<ServiceChecklistTemplate?> GetByIdAsync(int templateId);
    Task<IReadOnlyList<ServiceChecklistTemplateItem>> GetItemsByTemplateAsync(int templateId);
    Task AddCategoryToTemplateAsync(int templateId, int categoryId);
    Task<int> CreateTemplateAsync(ServiceChecklistTemplate template, IEnumerable<ServiceChecklistTemplateItem> items);
    Task UpdateTemplateAsync(ServiceChecklistTemplate template);
    Task UpsertItemsAsync(int templateId, IEnumerable<ServiceChecklistTemplateItem> items);
    Task DeleteItemsBatchAsync(int templateId, IEnumerable<int> categoryIds);
    Task SetActiveAsync(int templateId, bool isActive);
    Task DeleteTemplateAsync(int templateId);

    // New recommendation methods
    Task<IReadOnlyList<ServiceChecklistTemplate>> GetRecommendedTemplatesAsync(
        int currentKm,
        DateTime? lastMaintenanceDate,
        int? categoryId = null);
}






