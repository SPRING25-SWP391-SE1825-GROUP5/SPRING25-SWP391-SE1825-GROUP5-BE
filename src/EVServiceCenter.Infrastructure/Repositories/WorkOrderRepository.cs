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
    }
}


