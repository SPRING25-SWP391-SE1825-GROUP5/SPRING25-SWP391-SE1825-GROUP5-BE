using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
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

        public async Task<List<Inventory>> GetAllInventoriesAsync()
        {
            return await _context.Inventories
                .Include(i => i.Center)
                .Include(i => i.Part)
                .OrderByDescending(i => i.LastUpdated)
                .ToListAsync();
        }

        public async Task<Inventory> GetInventoryByIdAsync(int inventoryId)
        {
            return await _context.Inventories
                .Include(i => i.Center)
                .Include(i => i.Part)
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);
        }

        public async Task<Inventory> GetInventoryByCenterAndPartAsync(int centerId, int partId)
        {
            return await _context.Inventories
                .Include(i => i.Center)
                .Include(i => i.Part)
                .FirstOrDefaultAsync(i => i.CenterId == centerId && i.PartId == partId);
        }

        public async Task UpdateInventoryAsync(Inventory inventory)
        {
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> InventoryExistsAsync(int inventoryId)
        {
            return await _context.Inventories.AnyAsync(i => i.InventoryId == inventoryId);
        }

        public async Task<bool> IsCenterPartCombinationUniqueAsync(int centerId, int partId, int? excludeInventoryId = null)
        {
            var query = _context.Inventories.Where(i => i.CenterId == centerId && i.PartId == partId);
            
            if (excludeInventoryId.HasValue)
            {
                query = query.Where(i => i.InventoryId != excludeInventoryId.Value);
            }

            return !await query.AnyAsync();
        }
    }
}
