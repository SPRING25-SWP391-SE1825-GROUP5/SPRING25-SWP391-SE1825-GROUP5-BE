using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly EVDbContext _context;

        public ServiceRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Service>> GetAllServicesAsync()
        {
            return await _context.Services
                .Include(s => s.Category)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Service> GetServiceByIdAsync(int serviceId)
        {
            return await _context.Services
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId);
        }
    }
}
