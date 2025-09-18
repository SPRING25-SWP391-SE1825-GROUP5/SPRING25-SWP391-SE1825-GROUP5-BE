using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IServiceCategoryService
    {
        Task<List<ServiceCategoryResponse>> GetAllServiceCategoriesAsync();
        Task<List<ServiceCategoryResponse>> GetActiveServiceCategoriesAsync();
    }
}
