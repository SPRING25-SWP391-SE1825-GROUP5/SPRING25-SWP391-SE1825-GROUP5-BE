using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class WorkOrderPartRepository : IWorkOrderPartRepository
    {
        private readonly EVDbContext _db;
        public WorkOrderPartRepository(EVDbContext db) { _db = db; }

        public async Task<List<WorkOrderPart>> GetByWorkOrderIdAsync(int workOrderId)
        {
            return await _db.WorkOrderParts
                .Include(x => x.Part)
                .Where(x => x.WorkOrderId == workOrderId)
                .ToListAsync();
        }

        public async Task<WorkOrderPart> AddAsync(WorkOrderPart item)
        {
            var existing = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.WorkOrderId == item.WorkOrderId && x.PartId == item.PartId);
            if (existing != null)
            {
                // Upsert: cộng dồn số lượng, cập nhật đơn giá
                existing.QuantityUsed += item.QuantityUsed;
                existing.UnitCost = item.UnitCost;
                await _db.SaveChangesAsync();
                return existing;
            }

            _db.WorkOrderParts.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task<WorkOrderPart> UpdateAsync(WorkOrderPart item)
        {
            _db.WorkOrderParts.Update(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task DeleteAsync(int workOrderId, int partId)
        {
            var entity = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId && x.PartId == partId);
            if (entity != null)
            {
                _db.WorkOrderParts.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}


