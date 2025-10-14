using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IMaintenanceChecklistItemService
    {
        Task<MaintenanceChecklistItemListResponse> GetAllItemsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null);
        Task<MaintenanceChecklistItemResponse> GetItemByIdAsync(int itemId);
        Task<MaintenanceChecklistItemResponse> CreateItemAsync(CreateMaintenanceChecklistItemRequest request);
        Task<MaintenanceChecklistItemResponse> UpdateItemAsync(int itemId, UpdateMaintenanceChecklistItemRequest request);
        Task<bool> DeleteItemAsync(int itemId);
        Task<MaintenanceChecklistItemListResponse> GetTemplateByServiceIdAsync(int serviceId);
    }
}





