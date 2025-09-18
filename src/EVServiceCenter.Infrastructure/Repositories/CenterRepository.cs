using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class CenterRepository : ICenterRepository
    {
        private readonly EVDbContext _context;

        public CenterRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceCenter>> GetAllCentersAsync()
        {
            return await _context.ServiceCenters
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ServiceCenter>> GetActiveCentersAsync()
        {
            return await _context.ServiceCenters
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ServiceCenter> GetCenterByIdAsync(int centerId)
        {
            return await _context.ServiceCenters
                .FirstOrDefaultAsync(c => c.CenterId == centerId);
        }

        public async Task<ServiceCenter> CreateCenterAsync(ServiceCenter center)
        {
            _context.ServiceCenters.Add(center);
            await _context.SaveChangesAsync();
            return center;
        }

        public async Task UpdateCenterAsync(ServiceCenter center)
        {
            _context.ServiceCenters.Update(center);
            await _context.SaveChangesAsync();
        }
    }
}
