using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class WorkOrderPartRepository : IWorkOrderPartRepository
    {
        private readonly EVDbContext _db;
        public WorkOrderPartRepository(EVDbContext db) { _db = db; }

        public async Task<List<WorkOrderPart>> GetByBookingIdAsync(int bookingId)
        {
            return await _db.WorkOrderParts
                .Include(x => x.Part)
                .Where(x => x.BookingId == bookingId)
                .ToListAsync();
        }

        public async Task<List<WorkOrderPart>> GetByCenterAndDateRangeAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            return await _db.WorkOrderParts
                .Include(x => x.Part)
                .Include(x => x.Booking)
                .Where(x => x.Booking.CenterId == centerId && 
                           x.Booking.Status == "COMPLETED" &&
                           x.Booking.UpdatedAt >= startDate && 
                           x.Booking.UpdatedAt <= endDate)
                .ToListAsync();
        }

        public async Task<WorkOrderPart> AddAsync(WorkOrderPart item)
        {
            var existing = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.BookingId == item.BookingId && x.PartId == item.PartId);
            if (existing != null)
            {
                // Upsert: cộng dồn số lượng
                existing.QuantityUsed += item.QuantityUsed;
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

        public async Task DeleteAsync(int bookingId, int partId)
        {
            var entity = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.BookingId == bookingId && x.PartId == partId);
            if (entity != null)
            {
                _db.WorkOrderParts.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}


