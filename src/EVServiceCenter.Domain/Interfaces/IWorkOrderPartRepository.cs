using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IWorkOrderPartRepository
    {
        Task<List<WorkOrderPart>> GetByBookingIdAsync(int bookingId);
        Task<List<WorkOrderPart>> GetByCenterAndDateRangeAsync(int centerId, DateTime startDate, DateTime endDate);
        Task<WorkOrderPart> AddAsync(WorkOrderPart item);
        Task<WorkOrderPart> UpdateAsync(WorkOrderPart item);
        Task DeleteAsync(int bookingId, int partId);
    }
}


