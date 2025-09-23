using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly EVDbContext _db;
        public WorkOrderRepository(EVDbContext db) { _db = db; }

        public async Task<WorkOrder?> GetByBookingIdAsync(int bookingId)
        {
            return await _db.WorkOrders.FirstOrDefaultAsync(w => w.BookingId == bookingId);
        }

        public async Task<WorkOrder> CreateAsync(WorkOrder workOrder)
        {
            _db.WorkOrders.Add(workOrder);
            await _db.SaveChangesAsync();
            return workOrder;
        }
    }
}


