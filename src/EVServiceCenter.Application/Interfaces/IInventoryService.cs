using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IInventoryService
    {
        Task<InventoryListResponse> GetInventoriesAsync(int pageNumber = 1, int pageSize = 10, int? centerId = null, int? partId = null, string searchTerm = null);
        Task<InventoryResponse> GetInventoryByIdAsync(int inventoryId);
        Task<InventoryResponse> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request);
    }
}
