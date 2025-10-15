using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AddMaintenancePoliciesToServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int AddedPoliciesCount { get; set; }
        public List<MaintenancePolicyResponse> AddedPolicies { get; set; } = new List<MaintenancePolicyResponse>();
    }
}





