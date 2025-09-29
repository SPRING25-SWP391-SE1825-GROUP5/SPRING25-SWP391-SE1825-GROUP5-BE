using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenancePolicyRepository
    {
        Task<List<MaintenancePolicy>> GetByServiceIdAsync(int serviceId);
        Task<List<MaintenancePolicy>> GetActiveByServiceIdAsync(int serviceId);
    }
}


