using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IMaintenancePolicyService
    {
        Task<MaintenancePolicyListResponse> GetAllPoliciesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? serviceId = null);
        Task<MaintenancePolicyResponse> GetPolicyByIdAsync(int policyId);
        Task<MaintenancePolicyListResponse> GetActivePoliciesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? serviceId = null);
        Task<MaintenancePolicyResponse> CreatePolicyAsync(CreateMaintenancePolicyRequest request);
        Task<MaintenancePolicyResponse> UpdatePolicyAsync(int policyId, UpdateMaintenancePolicyRequest request);
        Task<bool> ToggleActiveAsync(int policyId);
        Task<bool> DeletePolicyAsync(int policyId);
        Task<MaintenancePolicyListResponse> GetPoliciesByServiceIdAsync(int serviceId);
    }
}

