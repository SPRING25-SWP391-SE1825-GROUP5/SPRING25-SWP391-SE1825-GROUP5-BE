using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IPartCategoryRepository
    {
        Task<List<PartCategory>> GetAllAsync();
        Task<List<PartCategory>> GetActiveAsync();
        Task<PartCategory?> GetByIdAsync(int categoryId);
    }
}

