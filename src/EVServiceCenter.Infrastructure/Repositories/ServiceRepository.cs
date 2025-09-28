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
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Service> GetServiceByIdAsync(int serviceId)
        {
            return await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId);
        }

        public async Task<List<Service>> GetActiveServicesAsync()
        {
            return await _context.Services
                .Where(s => s.IsActive == true)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Service> CreateServiceAsync(Service service)
        {
            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return service;
        }

        public async Task UpdateServiceAsync(Service service)
        {
            _context.Services.Update(service);
            await _context.SaveChangesAsync();
        }

    }
}
