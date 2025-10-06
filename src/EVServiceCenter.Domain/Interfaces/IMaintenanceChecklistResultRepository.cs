using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenanceChecklistResultRepository
    {
        Task<List<MaintenanceChecklistResult>> GetByChecklistIdAsync(int checklistId);
        Task UpsertAsync(MaintenanceChecklistResult result);
        Task UpsertManyAsync(IEnumerable<MaintenanceChecklistResult> results);
    }
}


