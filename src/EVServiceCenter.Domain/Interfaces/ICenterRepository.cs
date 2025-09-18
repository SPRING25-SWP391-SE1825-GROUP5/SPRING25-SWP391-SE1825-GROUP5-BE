using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ICenterRepository
    {
        Task<List<ServiceCenter>> GetAllCentersAsync();
        Task<List<ServiceCenter>> GetActiveCentersAsync();
        Task<ServiceCenter> GetCenterByIdAsync(int centerId);
        Task<ServiceCenter> CreateCenterAsync(ServiceCenter center);
        Task UpdateCenterAsync(ServiceCenter center);
    }
}
