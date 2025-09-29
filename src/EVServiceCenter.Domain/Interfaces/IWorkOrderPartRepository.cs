using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IWorkOrderPartRepository
    {
        Task<List<WorkOrderPart>> GetByWorkOrderIdAsync(int workOrderId);
        Task<WorkOrderPart> AddAsync(WorkOrderPart item);
        Task<WorkOrderPart> UpdateAsync(WorkOrderPart item);
        Task DeleteAsync(int workOrderId, int partId);
    }
}


