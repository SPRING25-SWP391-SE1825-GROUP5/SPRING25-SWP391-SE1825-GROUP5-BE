using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class ServicePartRepository : IServicePartRepository
    {
        private readonly EVDbContext _db;
        public ServicePartRepository(EVDbContext db) { _db = db; }

        public async Task<List<ServicePart>> GetByServiceIdAsync(int serviceId)
        {
            return await _db.ServiceParts
                .Include(sp => sp.Part)
                .Where(sp => sp.ServiceId == serviceId)
                .ToListAsync();
        }

        public async Task<List<ServicePart>> GetByPartIdAsync(int partId)
        {
            return await _db.ServiceParts
                .Include(sp => sp.Service)
                .Where(sp => sp.PartId == partId)
                .ToListAsync();
        }

        public async Task ReplaceForServiceAsync(int serviceId, IEnumerable<ServicePart> items)
        {
            var existing = await _db.ServiceParts.Where(x => x.ServiceId == serviceId).ToListAsync();
            if (existing.Count > 0)
            {
                _db.ServiceParts.RemoveRange(existing);
            }
            if (items != null)
            {
                await _db.ServiceParts.AddRangeAsync(items);
            }
            await _db.SaveChangesAsync();
        }

        public async Task AddAsync(ServicePart item)
        {
            _db.ServiceParts.Add(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int servicePartId)
        {
            var entity = await _db.ServiceParts.FirstOrDefaultAsync(x => x.ServicePartId == servicePartId);
            if (entity != null)
            {
                _db.ServiceParts.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteByServiceAndPartAsync(int serviceId, int partId)
        {
            var entity = await _db.ServiceParts.FirstOrDefaultAsync(x => x.ServiceId == serviceId && x.PartId == partId);
            if (entity != null)
            {
                _db.ServiceParts.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}


