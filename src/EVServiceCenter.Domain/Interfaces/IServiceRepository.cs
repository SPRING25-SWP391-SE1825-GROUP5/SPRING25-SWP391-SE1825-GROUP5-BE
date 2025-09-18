using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IServiceRepository
    {
        Task<List<Service>> GetAllServicesAsync();
        Task<Service> GetServiceByIdAsync(int serviceId);
    }
}
