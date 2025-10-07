using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenanceChecklistItemRepository
    {
        Task<List<MaintenanceChecklistItem>> GetAllItemsAsync();
        Task<MaintenanceChecklistItem> GetItemByIdAsync(int itemId);
        Task<MaintenanceChecklistItem> CreateItemAsync(MaintenanceChecklistItem item);
        Task UpdateItemAsync(MaintenanceChecklistItem item);
        Task DeleteItemAsync(int itemId);
        Task<bool> ItemExistsAsync(int itemId);
        Task<List<MaintenanceChecklistItem>> GetTemplateByServiceIdAsync(int serviceId);
    }
}


