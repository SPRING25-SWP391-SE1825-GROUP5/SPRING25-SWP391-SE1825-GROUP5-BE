using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IWorkOrderRepository
    {
        Task<WorkOrder?> GetByBookingIdAsync(int bookingId);
        Task<WorkOrder> CreateAsync(WorkOrder workOrder);
        Task<WorkOrder?> GetByIdAsync(int id);
        Task UpdateAsync(WorkOrder workOrder);
    }
}


