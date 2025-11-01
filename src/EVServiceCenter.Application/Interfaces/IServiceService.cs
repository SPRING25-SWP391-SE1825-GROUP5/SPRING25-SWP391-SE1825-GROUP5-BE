using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IServiceService
    {
        Task<ServiceListResponse> GetAllServicesAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? categoryId = null);
        Task<ServiceResponse> GetServiceByIdAsync(int serviceId);
        Task<ServiceListResponse> GetActiveServicesAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? categoryId = null);
        Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request);
        Task<ServiceResponse> UpdateServiceAsync(int serviceId, UpdateServiceRequest request);
        Task<bool> ToggleActiveAsync(int serviceId);
        Task<int> GetServicesCountAsync();
        System.Threading.Tasks.Task<System.Collections.Generic.IList<ServiceResponse>> GetServicesForExportAsync(int maxRecords = 100000);
    }
}
