using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IInventoryRepository
    {
        // Inventory methods
        Task<List<Inventory>> GetAllInventoriesAsync();
        Task<Inventory?> GetInventoryByIdAsync(int inventoryId);
        Task<Inventory?> GetInventoryByCenterIdAsync(int centerId);
        Task<Inventory> AddInventoryAsync(Inventory inventory);
        Task UpdateInventoryAsync(Inventory inventory);
        Task<bool> CenterHasInventoryAsync(int centerId);
        
        // InventoryPart methods
        Task<List<InventoryPart>> GetAllInventoryPartsAsync();
        Task<List<InventoryPart>> GetInventoryPartsByInventoryIdAsync(int inventoryId);
        Task<InventoryPart?> GetInventoryPartByInventoryAndPartAsync(int inventoryId, int partId);
        Task<InventoryPart> AddInventoryPartAsync(InventoryPart inventoryPart);
        Task UpdateInventoryPartAsync(InventoryPart inventoryPart);
        Task DeleteInventoryPartAsync(int inventoryId, int partId);
        Task<bool> InventoryPartExistsAsync(int inventoryId, int partId);
        
        // Validation methods
        Task<ServiceCenter?> GetCenterByIdAsync(int centerId);
        Task<Part?> GetPartByIdAsync(int partId);
    }
}
