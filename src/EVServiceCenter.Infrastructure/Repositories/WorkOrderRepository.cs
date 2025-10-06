using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly EVDbContext _db;
        public WorkOrderRepository(EVDbContext db) { _db = db; }

        public async Task<WorkOrder?> GetByBookingIdAsync(int bookingId)
        {
            return await _db.WorkOrders
                .Where(w => w.BookingId == bookingId)
                .OrderByDescending(w => w.CreatedAt)
                .Include(w => w.Technician).ThenInclude(t => t.User)
                .Include(w => w.Booking)
                .FirstOrDefaultAsync();
        }

        public async Task<WorkOrder> CreateAsync(WorkOrder workOrder)
        {
            _db.WorkOrders.Add(workOrder);
            await _db.SaveChangesAsync();
            return workOrder;
        }

        public async Task<WorkOrder?> GetByIdAsync(int id)
        {
            return await _db.WorkOrders
                .Include(w => w.WorkOrderParts).ThenInclude(p => p.Part)
                .FirstOrDefaultAsync(w => w.WorkOrderId == id);
        }

        public async Task UpdateAsync(WorkOrder workOrder)
        {
            _db.WorkOrders.Update(workOrder);
            await _db.SaveChangesAsync();
        }

        public async Task<(System.Collections.Generic.List<WorkOrder> items, int total)> QueryAsync(int? centerId, int? technicianId, int? customerId, int? vehicleId, int? serviceId, string status, System.DateTime? from, System.DateTime? to, int page, int size, string sort, bool includeRelations = false)
        {
            var query = _db.WorkOrders.AsQueryable();
            if (includeRelations)
            {
                query = query
                    .Include(w => w.Technician).ThenInclude(t => t.User)
                    .Include(w => w.Booking)
                    .Include(w => w.WorkOrderParts).ThenInclude(p => p.Part);
            }
            if (centerId.HasValue) query = query.Where(w => w.CenterId == centerId);
            if (technicianId.HasValue) query = query.Where(w => w.TechnicianId == technicianId);
            if (customerId.HasValue) query = query.Where(w => w.CustomerId == customerId);
            if (vehicleId.HasValue) query = query.Where(w => w.VehicleId == vehicleId);
            if (serviceId.HasValue) query = query.Where(w => w.ServiceId == serviceId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(w => w.Status == status);
            if (from.HasValue) query = query.Where(w => w.CreatedAt >= from);
            if (to.HasValue) query = query.Where(w => w.CreatedAt <= to);

            query = sort?.ToLower() switch
            {
                "createdat_asc" => query.OrderBy(w => w.CreatedAt),
                "updatedat_desc" => query.OrderByDescending(w => w.UpdatedAt),
                "updatedat_asc" => query.OrderBy(w => w.UpdatedAt),
                _ => query.OrderByDescending(w => w.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();
            return (items, total);
        }

        public async Task<System.Collections.Generic.List<WorkOrder>> GetByCenterAsync(int centerId, System.DateTime? from, System.DateTime? to, string status)
        {
            return await _db.WorkOrders.Where(w => w.CenterId == centerId)
                .Where(w => string.IsNullOrEmpty(status) || w.Status == status)
                .Where(w => !from.HasValue || w.CreatedAt >= from)
                .Where(w => !to.HasValue || w.CreatedAt <= to)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.List<WorkOrder>> GetByTechnicianAsync(int technicianId, System.DateTime? from, System.DateTime? to, string status)
        {
            return await _db.WorkOrders.Where(w => w.TechnicianId == technicianId)
                .Where(w => string.IsNullOrEmpty(status) || w.Status == status)
                .Where(w => !from.HasValue || w.CreatedAt >= from)
                .Where(w => !to.HasValue || w.CreatedAt <= to)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.List<WorkOrder>> GetByCustomerAsync(int customerId, System.DateTime? from, System.DateTime? to, string status)
        {
            return await _db.WorkOrders.Where(w => w.CustomerId == customerId)
                .Where(w => string.IsNullOrEmpty(status) || w.Status == status)
                .Where(w => !from.HasValue || w.CreatedAt >= from)
                .Where(w => !to.HasValue || w.CreatedAt <= to)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.List<WorkOrder>> GetByCustomerVehicleAsync(int customerId, int vehicleId, System.DateTime? from, System.DateTime? to, string status)
        {
            return await _db.WorkOrders.Where(w => w.CustomerId == customerId && w.VehicleId == vehicleId)
                .Where(w => string.IsNullOrEmpty(status) || w.Status == status)
                .Where(w => !from.HasValue || w.CreatedAt >= from)
                .Where(w => !to.HasValue || w.CreatedAt <= to)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<object> GetStatisticsAsync(int? centerId, System.DateTime? from, System.DateTime? to, string groupBy)
        {
            var query = _db.WorkOrders.AsQueryable();
            if (centerId.HasValue) query = query.Where(w => w.CenterId == centerId);
            if (from.HasValue) query = query.Where(w => w.CreatedAt >= from);
            if (to.HasValue) query = query.Where(w => w.CreatedAt <= to);

            var total = await query.CountAsync();
            var byStatus = await query.GroupBy(w => w.Status).Select(g => new { status = g.Key, count = g.Count() }).ToListAsync();
            var byTechnician = await query.Where(w => w.TechnicianId != null).GroupBy(w => w.TechnicianId).Select(g => new { technicianId = g.Key, count = g.Count() }).ToListAsync();

            return new { total, byStatus, byTechnician };
        }

        public async Task<WorkOrder> GetLastCompletedByVehicleAsync(int vehicleId)
        {
            return await _db.WorkOrders
                .Where(w => w.VehicleId == vehicleId && w.Status == "COMPLETED")
                .OrderByDescending(w => w.UpdatedAt)
                .FirstOrDefaultAsync();
        }
    }
}


