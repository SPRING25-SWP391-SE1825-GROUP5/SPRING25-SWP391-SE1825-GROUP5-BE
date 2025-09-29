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
        public MaintenanceChecklistItemRepository(EVDbContext db) { }

        public Task<List<MaintenanceChecklistItem>> GetTemplateByServiceIdAsync(int serviceId)
        {
            // Bảng template đã bỏ; trả rỗng để tránh dùng nhầm.
            return Task.FromResult(new List<MaintenanceChecklistItem>());
        }
    }
}


