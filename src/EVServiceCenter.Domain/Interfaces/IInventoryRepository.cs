using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IInventoryRepository
    {
        Task<List<Inventory>> GetAllInventoriesAsync();
        Task<Inventory> GetInventoryByIdAsync(int inventoryId);
        Task<Inventory> GetInventoryByCenterAndPartAsync(int centerId, int partId);
        Task<List<Inventory>> GetByCenterAndPartIdsAsync(int centerId, List<int> partIds);
        Task<List<Inventory>> GetByPartIdsAsync(List<int> partIds);
        Task UpdateInventoryAsync(Inventory inventory);
        Task<Inventory> AddInventoryAsync(Inventory inventory);
        Task<bool> InventoryExistsAsync(int inventoryId);
        Task<bool> IsCenterPartCombinationUniqueAsync(int centerId, int partId, int? excludeInventoryId = null);
    }
}
