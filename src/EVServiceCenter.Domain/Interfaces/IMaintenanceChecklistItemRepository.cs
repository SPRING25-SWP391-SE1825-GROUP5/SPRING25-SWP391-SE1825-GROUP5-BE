using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenanceChecklistItemRepository
    {
        Task<List<MaintenanceChecklistItem>> GetTemplateByServiceIdAsync(int serviceId);
    }
}


