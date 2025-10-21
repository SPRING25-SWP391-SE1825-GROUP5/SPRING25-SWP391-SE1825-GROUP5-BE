using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly EVDbContext _context;

        public InventoryRepository(EVDbContext context)
        {
            _context = context;
        }

        // Inventory methods (cấu trúc cũ)
        public async Task<List<Inventory>> GetAllInventoriesAsync()
        {
            return await _context.Inventories
                .Include(i => i.Center)
                .Include(i => i.InventoryParts)
                    .ThenInclude(ip => ip.Part)
                .OrderByDescending(i => i.LastUpdated)
                .ToListAsync();
        }

        public async Task<Inventory?> GetInventoryByIdAsync(int inventoryId)
        {
            return await _context.Inventories
                .Include(i => i.Center)
                .Include(i => i.InventoryParts)
                    .ThenInclude(ip => ip.Part)
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);
        }

        public async Task<Inventory?> GetInventoryByCenterIdAsync(int centerId)
        {
            return await _context.Inventories
                .Include(i => i.Center)
                .Include(i => i.InventoryParts)
                    .ThenInclude(ip => ip.Part)
                .FirstOrDefaultAsync(i => i.CenterId == centerId);
        }


        public async Task<Inventory> AddInventoryAsync(Inventory inventory)
        {
            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();
            return inventory;
        }

        public async Task UpdateInventoryAsync(Inventory inventory)
        {
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CenterHasInventoryAsync(int centerId)
        {
            return await _context.Inventories.AnyAsync(i => i.CenterId == centerId);
        }

        // InventoryPart methods (cấu trúc mới)
        public async Task<List<InventoryPart>> GetInventoryPartsByInventoryIdAsync(int inventoryId)
        {
            return await _context.InventoryParts
                .Include(ip => ip.Part)
                .Where(ip => ip.InventoryId == inventoryId)
                .OrderBy(ip => ip.Part.PartName)
                .ToListAsync();
        }

        public async Task<InventoryPart?> GetInventoryPartByInventoryAndPartAsync(int inventoryId, int partId)
        {
            return await _context.InventoryParts
                .Include(ip => ip.Part)
                .FirstOrDefaultAsync(ip => ip.InventoryId == inventoryId && ip.PartId == partId);
        }

        public async Task<InventoryPart> AddInventoryPartAsync(InventoryPart inventoryPart)
        {
            _context.InventoryParts.Add(inventoryPart);
            await _context.SaveChangesAsync();
            return inventoryPart;
        }

        public async Task UpdateInventoryPartAsync(InventoryPart inventoryPart)
        {
            _context.InventoryParts.Update(inventoryPart);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInventoryPartAsync(int inventoryId, int partId)
        {
            var inventoryPart = await _context.InventoryParts
                .FirstOrDefaultAsync(ip => ip.InventoryId == inventoryId && ip.PartId == partId);
            
            if (inventoryPart != null)
            {
                _context.InventoryParts.Remove(inventoryPart);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> InventoryPartExistsAsync(int inventoryId, int partId)
        {
            return await _context.InventoryParts
                .AnyAsync(ip => ip.InventoryId == inventoryId && ip.PartId == partId);
        }

        // Validation methods
        public async Task<ServiceCenter?> GetCenterByIdAsync(int centerId)
        {
            return await _context.ServiceCenters
                .FirstOrDefaultAsync(c => c.CenterId == centerId);
        }

        public async Task<Part?> GetPartByIdAsync(int partId)
        {
            return await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == partId);
        }
    }
}
