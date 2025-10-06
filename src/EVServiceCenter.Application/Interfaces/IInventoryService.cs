using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IInventoryService
    {
        // Inventory management
        Task<InventoryListResponse> GetInventoriesAsync(int pageNumber = 1, int pageSize = 10, int? centerId = null, string searchTerm = null);
        Task<InventoryListResponse> GetInventoriesByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string searchTerm = null);
        Task<InventoryResponse> GetInventoryByIdAsync(int inventoryId);
        Task<InventoryResponse> CreateInventoryAsync(CreateInventoryRequest request);
        
        // InventoryPart management
        Task<InventoryPartResponse> AddPartToInventoryAsync(int inventoryId, int partId, int currentStock, int minimumStock);
        Task<InventoryPartResponse> UpdateInventoryPartAsync(int inventoryId, int partId, int currentStock, int minimumStock);
        Task<bool> RemovePartFromInventoryAsync(int inventoryId, int partId);

        // Availability methods
        Task<List<InventoryPartResponse>> GetAvailabilityAsync(int centerId, List<int> partIds);
        Task<List<InventoryAvailabilityResponse>> GetGlobalAvailabilityAsync(List<int> partIds);
        Task<List<InventoryAvailabilityResponse>> GetGlobalAvailabilityAllAsync();
    }
}
