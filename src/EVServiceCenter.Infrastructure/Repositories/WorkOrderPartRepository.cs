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

        public async Task<WorkOrderPart?> GetByIdAsync(int workOrderPartId)
        {
            return await _db.WorkOrderParts
                .Include(x => x.Part)
                .Include(x => x.Booking)
                .FirstOrDefaultAsync(x => x.WorkOrderPartId == workOrderPartId);
        }

        public async Task<WorkOrderPart> AddAsync(WorkOrderPart item)
        {
            // Nếu status là PENDING_CUSTOMER_APPROVAL (từ FAIL evaluation), không cộng dồn với existing
            // Vì đây là yêu cầu thay thế cụ thể, không phải thêm số lượng
            if (item.Status != "PENDING_CUSTOMER_APPROVAL")
            {
                var existing = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.BookingId == item.BookingId && x.PartId == item.PartId);
                if (existing != null)
                {
                    // Upsert: cộng dồn số lượng
                    existing.QuantityUsed += item.QuantityUsed;
                    await _db.SaveChangesAsync();
                    return existing;
                }
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

        public async Task<bool> DeleteByIdAsync(int workOrderPartId)
        {
            var entity = await _db.WorkOrderParts
                .FirstOrDefaultAsync(x => x.WorkOrderPartId == workOrderPartId);
            if (entity == null)
                return false;

            _db.WorkOrderParts.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string? Error, WorkOrderPart? Item)> ApproveAsync(int id, int centerId, int approvedByUserId, DateTime approvedAtUtc)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var entity = await _db.WorkOrderParts.Include(x => x.Part).FirstOrDefaultAsync(x => x.WorkOrderPartId == id);
                if (entity == null) return (false, "NOT_FOUND", null);

                var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == entity.BookingId);
                if (booking == null) return (false, "BOOKING_NOT_FOUND", null);
                if (booking.CenterId != centerId) return (false, "CENTER_MISMATCH", null);

            // Phê duyệt KHÔNG trừ kho, KHÔNG tiêu thụ. Chỉ ghi nhận ApprovedByStaffId.
            // Việc trừ kho và tiêu thụ sẽ được thực hiện ở bước ConsumeWithInventoryAsync.
            entity.ApprovedByStaffId = approvedByUserId;
                // Không đổi Status tại đây (giữ nguyên DRAFT hoặc trạng thái hiện tại)
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return (true, null, entity);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<WorkOrderPart?> RejectAsync(int id, int rejectedByUserId, DateTime rejectedAtUtc)
        {
            var entity = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.WorkOrderPartId == id);
            if (entity == null) return null;
            entity.Status = "REJECTED";
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<WorkOrderPart?> CustomerApproveAsync(int id)
        {
            var entity = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.WorkOrderPartId == id);
            if (entity == null) return null;
            if (entity.Status != "PENDING_CUSTOMER_APPROVAL") return null;
            entity.Status = "DRAFT";
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<WorkOrderPart?> CustomerRejectAsync(int id)
        {
            var entity = await _db.WorkOrderParts.FirstOrDefaultAsync(x => x.WorkOrderPartId == id);
            if (entity == null) return null;
            if (entity.Status != "PENDING_CUSTOMER_APPROVAL") return null;
            entity.Status = "REJECTED";
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<(bool Success, string? Error, WorkOrderPart? Item)> ConsumeWithInventoryAsync(int id, int centerId, DateTime consumedAtUtc, int consumedByUserId)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            var entity = await _db.WorkOrderParts.Include(x => x.Part).FirstOrDefaultAsync(x => x.WorkOrderPartId == id);
            if (entity == null) return (false, "NOT_FOUND", null);

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingId == entity.BookingId);
            if (booking == null) return (false, "BOOKING_NOT_FOUND", null);
            if (booking.CenterId != centerId) return (false, "CENTER_MISMATCH", null);

            var inventory = await _db.Inventories.Include(i => i.InventoryParts).FirstOrDefaultAsync(i => i.CenterId == centerId);
            if (inventory == null) return (false, "INVENTORY_NOT_FOUND", null);
            var invPart = inventory.InventoryParts.FirstOrDefault(ip => ip.PartId == entity.PartId);
            if (invPart == null) return (false, "PART_NOT_IN_INVENTORY", null);
            if (invPart.CurrentStock < entity.QuantityUsed) return (false, "INSUFFICIENT_STOCK", null);

            invPart.CurrentStock -= entity.QuantityUsed;
            invPart.LastUpdated = consumedAtUtc;
            inventory.LastUpdated = consumedAtUtc;
            await _db.SaveChangesAsync();

            entity.Status = "CONSUMED";
            entity.ConsumedAt = consumedAtUtc;
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return (true, null, entity);
        }
    }
}


