using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EVServiceCenter.Infrastructure.Repositories;

public class ServiceCategoryRepository : IServiceCategoryRepository
{
    private readonly EVDbContext _context;

    public ServiceCategoryRepository(EVDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy tất cả danh mục dịch vụ
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ</returns>
    public async Task<IEnumerable<ServiceCategory>> GetAllAsync()
    {
        return await _context.ServiceCategories
            .OrderBy(sc => sc.CategoryName)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh mục dịch vụ đang hoạt động
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ đang hoạt động</returns>
    public async Task<IEnumerable<ServiceCategory>> GetActiveAsync()
    {
        return await _context.ServiceCategories
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.CategoryName)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh mục dịch vụ theo ID
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <returns>Danh mục dịch vụ</returns>
    public async Task<ServiceCategory?> GetByIdAsync(int id)
    {
        return await _context.ServiceCategories
            .FirstOrDefaultAsync(sc => sc.CategoryId == id);
    }

    /// <summary>
    /// Tạo danh mục dịch vụ mới
    /// </summary>
    /// <param name="serviceCategory">Thông tin danh mục mới</param>
    /// <returns>Danh mục đã tạo</returns>
    public async Task<ServiceCategory> CreateAsync(ServiceCategory serviceCategory)
    {
        serviceCategory.CreatedAt = DateTime.UtcNow;
        _context.ServiceCategories.Add(serviceCategory);
        await _context.SaveChangesAsync();
        return serviceCategory;
    }

    /// <summary>
    /// Cập nhật danh mục dịch vụ
    /// </summary>
    /// <param name="serviceCategory">Thông tin danh mục cần cập nhật</param>
    /// <returns>Danh mục đã cập nhật</returns>
    public async Task<ServiceCategory> UpdateAsync(ServiceCategory serviceCategory)
    {
        _context.ServiceCategories.Update(serviceCategory);
        await _context.SaveChangesAsync();
        return serviceCategory;
    }

    /// <summary>
    /// Thay đổi trạng thái hoạt động của danh mục
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <param name="isActive">Trạng thái mới</param>
    /// <returns>True nếu thành công</returns>
    public async Task<bool> ToggleActiveAsync(int id, bool isActive)
    {
        var serviceCategory = await _context.ServiceCategories
            .FirstOrDefaultAsync(sc => sc.CategoryId == id);

        if (serviceCategory == null)
            return false;

        serviceCategory.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Kiểm tra tên danh mục có tồn tại không
    /// </summary>
    /// <param name="categoryName">Tên danh mục</param>
    /// <param name="excludeId">ID danh mục cần loại trừ (dùng khi cập nhật)</param>
    /// <returns>True nếu tên đã tồn tại</returns>
    public async Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null)
    {
        var query = _context.ServiceCategories
            .Where(sc => sc.CategoryName.ToLower() == categoryName.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(sc => sc.CategoryId != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
