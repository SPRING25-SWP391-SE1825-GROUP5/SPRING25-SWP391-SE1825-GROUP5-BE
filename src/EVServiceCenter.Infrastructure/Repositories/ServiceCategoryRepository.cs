using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class ServiceCategoryRepository : IServiceCategoryRepository
    {
        private readonly EVDbContext _context;

        public ServiceCategoryRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceCategory>> GetAllServiceCategoriesAsync()
        {
            return await _context.ServiceCategories
                .Include(sc => sc.ParentCategory)
                .OrderBy(sc => sc.CategoryName)
                .ToListAsync();
        }

        public async Task<List<ServiceCategory>> GetActiveServiceCategoriesAsync()
        {
            return await _context.ServiceCategories
                .Include(sc => sc.ParentCategory)
                .Where(sc => sc.IsActive)
                .OrderBy(sc => sc.CategoryName)
                .ToListAsync();
        }
    }
}
