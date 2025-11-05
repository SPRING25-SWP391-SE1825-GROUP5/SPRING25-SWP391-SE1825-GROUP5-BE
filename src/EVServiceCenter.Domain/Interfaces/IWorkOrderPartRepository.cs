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
        Task<WorkOrderPart?> GetByIdAsync(int workOrderPartId);
        Task<WorkOrderPart> AddAsync(WorkOrderPart item);
        Task<WorkOrderPart> UpdateAsync(WorkOrderPart item);
        Task DeleteAsync(int bookingId, int partId);

        Task<(bool Success, string? Error, WorkOrderPart? Item)> ApproveAsync(int id, int centerId, int approvedByUserId, DateTime approvedAtUtc);
        Task<WorkOrderPart?> RejectAsync(int id, int rejectedByUserId, DateTime rejectedAtUtc);
        Task<WorkOrderPart?> CustomerApproveAsync(int id);
        Task<WorkOrderPart?> CustomerRejectAsync(int id);
        Task<(bool Success, string? Error, WorkOrderPart? Item)> ConsumeWithInventoryAsync(int id, int centerId, DateTime consumedAtUtc, int consumedByUserId);
    }
}


