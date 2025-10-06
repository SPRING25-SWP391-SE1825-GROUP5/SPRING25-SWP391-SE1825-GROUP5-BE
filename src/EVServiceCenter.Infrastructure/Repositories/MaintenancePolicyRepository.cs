using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using EVServiceCenter.Domain.Configurations;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenancePolicyRepository : IMaintenancePolicyRepository
    {
        private readonly EVDbContext _db;

        public MaintenancePolicyRepository(EVDbContext db)
        {
            _db = db;
        }

        public async Task<List<MaintenancePolicy>> GetByServiceIdAsync(int serviceId)
        {
            return await _db.MaintenancePolicies
                .Where(p => p.ServiceId == serviceId)
                .ToListAsync();
        }

        public async Task<List<MaintenancePolicy>> GetActiveByServiceIdAsync(int serviceId)
        {
            return await _db.MaintenancePolicies
                .Where(p => p.ServiceId == serviceId && p.IsActive)
                .ToListAsync();
        }
    }
}


