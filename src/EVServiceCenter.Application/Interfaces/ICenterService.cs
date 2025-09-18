using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ICenterService
    {
        Task<CenterListResponse> GetAllCentersAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string city = null);
        Task<CenterListResponse> GetActiveCentersAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string city = null);
        Task<CenterResponse> GetCenterByIdAsync(int centerId);
        Task<CenterResponse> CreateCenterAsync(CreateCenterRequest request);
        Task<CenterResponse> UpdateCenterAsync(int centerId, UpdateCenterRequest request);
    }
}
