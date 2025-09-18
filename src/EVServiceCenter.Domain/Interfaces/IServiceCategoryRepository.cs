using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IServiceCategoryRepository
    {
        Task<List<ServiceCategory>> GetAllServiceCategoriesAsync();
        Task<List<ServiceCategory>> GetActiveServiceCategoriesAsync();
    }
}
