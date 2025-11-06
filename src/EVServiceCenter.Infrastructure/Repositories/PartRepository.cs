using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class PartRepository : IPartRepository
    {
        private readonly EVDbContext _context;

        public PartRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Part>> GetAllPartsAsync()
        {
            return await _context.Parts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Part?> GetPartByIdAsync(int partId)
        {
            return await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == partId);
        }

        public async Task<Part?> GetPartLiteByIdAsync(int partId)
        {
            // Chỉ lấy các trường cần thiết để tránh Null đọc phải ở các navigation không cần
            return await _context.Parts
                .AsNoTracking()
                .Select(p => new Part
                {
                    PartId = p.PartId,
                    PartNumber = p.PartNumber,
                    PartName = p.PartName,
                    Brand = p.Brand,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync(p => p.PartId == partId);
        }

        public async Task<Part> CreatePartAsync(Part part)
        {
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
            return part;
        }

        public async Task UpdatePartAsync(Part part)
        {
            _context.Parts.Update(part);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsPartNumberUniqueAsync(string partNumber, int? excludePartId = null)
        {
            var query = _context.Parts.Where(p => p.PartNumber == partNumber);

            if (excludePartId.HasValue)
            {
                query = query.Where(p => p.PartId != excludePartId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> PartExistsAsync(int partId)
        {
            return await _context.Parts.AnyAsync(p => p.PartId == partId);
        }

        public async Task<List<Part>> GetPartsByCategoryIdAsync(int categoryId)
        {
            return await _context.PartCategoryMaps
                .AsNoTracking()
                .Where(pcm => pcm.CategoryId == categoryId)
                .Include(pcm => pcm.Part)
                .Where(pcm => pcm.Part != null && pcm.Part.IsActive)
                .Select(pcm => pcm.Part!)
                .ToListAsync();
        }

        public async Task<int?> GetFirstCategoryIdForPartAsync(int partId)
        {
            return await _context.PartCategoryMaps
                .AsNoTracking()
                .Where(pcm => pcm.PartId == partId)
                .Select(pcm => (int?)pcm.CategoryId)
                .FirstOrDefaultAsync();
        }
    }
}
