using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenancePolicyRepository
    {
        Task<List<MaintenancePolicy>> GetAllPoliciesAsync();
        Task<MaintenancePolicy> GetPolicyByIdAsync(int policyId);
        Task<List<MaintenancePolicy>> GetActivePoliciesAsync();
        Task<List<MaintenancePolicy>> GetPoliciesByServiceIdAsync(int serviceId);
        Task<List<MaintenancePolicy>> GetActiveByServiceIdAsync(int serviceId);
        Task<MaintenancePolicy> CreatePolicyAsync(MaintenancePolicy policy);
        Task UpdatePolicyAsync(MaintenancePolicy policy);
        Task DeletePolicyAsync(int policyId);
        Task<bool> PolicyExistsAsync(int policyId);
    }
}