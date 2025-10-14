using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IServicePartRepository
    {
        Task<List<ServicePart>> GetByServiceIdAsync(int serviceId);
        Task<List<ServicePart>> GetByPartIdAsync(int partId);
        Task ReplaceForServiceAsync(int serviceId, IEnumerable<ServicePart> items);
        Task AddAsync(ServicePart item);
        Task DeleteAsync(int servicePartId);
        Task DeleteByServiceAndPartAsync(int serviceId, int partId);
    }
}


