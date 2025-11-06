using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class PartCategoryRepository : IPartCategoryRepository
    {
        private readonly EVDbContext _context;

        public PartCategoryRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<PartCategory>> GetAllAsync()
        {
            return await _context.PartCategories
                .Include(pc => pc.Parent)
                .OrderBy(pc => pc.CategoryName)
                .ToListAsync();
        }

        public async Task<List<PartCategory>> GetActiveAsync()
        {
            return await _context.PartCategories
                .Where(pc => pc.IsActive)
                .Include(pc => pc.Parent)
                .OrderBy(pc => pc.CategoryName)
                .ToListAsync();
        }

        public async Task<PartCategory?> GetByIdAsync(int categoryId)
        {
            return await _context.PartCategories
                .Include(pc => pc.Parent)
                .Include(pc => pc.Children)
                .FirstOrDefaultAsync(pc => pc.CategoryId == categoryId);
        }
    }
}

