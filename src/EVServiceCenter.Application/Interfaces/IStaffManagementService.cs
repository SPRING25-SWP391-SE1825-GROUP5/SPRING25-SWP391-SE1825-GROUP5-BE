using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IStaffManagementService
    {
        // Staff Management
        Task<StaffResponse> AddStaffToCenterAsync(AddStaffToCenterRequest request);
        Task<StaffResponse> GetStaffByIdAsync(int staffId);
        Task<StaffListResponse> GetStaffByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? position = null, bool? isActive = null);
        Task<StaffListResponse> GetAllStaffAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? position = null, bool? isActive = null);
        Task<StaffResponse> UpdateStaffAsync(int staffId, UpdateStaffRequest request);
        Task<bool> RemoveStaffFromCenterAsync(int staffId);

        // Technician Management
        Task<TechnicianResponse> AddTechnicianToCenterAsync(AddTechnicianToCenterRequest request);
        Task<TechnicianResponse> GetTechnicianByIdAsync(int technicianId);
        Task<TechnicianListResponse> GetTechniciansByCenterAsync(int centerId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? specialization = null, bool? isActive = null);
        Task<TechnicianResponse> UpdateTechnicianAsync(int technicianId, UpdateTechnicianRequest request);
        Task<bool> RemoveTechnicianFromCenterAsync(int technicianId);

        // Validation
        Task<bool> IsUserAlreadyStaffAsync(int userId);
        Task<bool> IsUserAlreadyTechnicianAsync(int userId);
        Task<bool> CanUserBeAssignedToCenterAsync(int userId, int centerId);
        Task<bool> CanUserBeAssignedAsStaffAsync(int userId, int centerId);
        Task<bool> CanUserBeAssignedAsTechnicianAsync(int userId, int centerId);
    }
}
