using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenanceChecklistItemRepository : IMaintenanceChecklistItemRepository
    {
        private readonly EVDbContext _context;

        public MaintenanceChecklistItemRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<MaintenanceChecklistItem>> GetAllItemsAsync()
        {
            return await _context.MaintenanceChecklistItems
                .OrderByDescending(i => i.ItemId)
                .ToListAsync();
        }

        public async Task<MaintenanceChecklistItem> GetItemByIdAsync(int itemId)
        {
            return await _context.MaintenanceChecklistItems
                .FirstOrDefaultAsync(i => i.ItemId == itemId);
        }

        public async Task<MaintenanceChecklistItem> CreateItemAsync(MaintenanceChecklistItem item)
        {
            _context.MaintenanceChecklistItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task UpdateItemAsync(MaintenanceChecklistItem item)
        {
            _context.MaintenanceChecklistItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var item = await _context.MaintenanceChecklistItems.FindAsync(itemId);
            if (item != null)
            {
                _context.MaintenanceChecklistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ItemExistsAsync(int itemId)
        {
            return await _context.MaintenanceChecklistItems.AnyAsync(i => i.ItemId == itemId);
        }

        public Task<List<MaintenanceChecklistItem>> GetTemplateByServiceIdAsync(int serviceId)
        {
            // Bảng template đã bỏ; trả rỗng để tránh dùng nhầm.
            return Task.FromResult(new List<MaintenanceChecklistItem>());
        }
    }
}


