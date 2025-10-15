using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPartService
    {
        Task<PartListResponse> GetAllPartsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, bool? isActive = null);
        Task<PartResponse> GetPartByIdAsync(int partId);
        Task<PartResponse> CreatePartAsync(CreatePartRequest request);
        Task<PartResponse> UpdatePartAsync(int partId, UpdatePartRequest request);
        Task<List<ServiceCompatibilityResponse>> GetServicesByPartIdAsync(int partId);
    }
}
