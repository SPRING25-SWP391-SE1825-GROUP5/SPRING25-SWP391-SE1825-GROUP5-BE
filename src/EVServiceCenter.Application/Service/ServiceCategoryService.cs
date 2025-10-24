using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EVServiceCenter.Application.Service;

public class ServiceCategoryService : IServiceCategoryService
{
    private readonly IServiceCategoryRepository _serviceCategoryRepository;

    public ServiceCategoryService(IServiceCategoryRepository serviceCategoryRepository)
    {
        _serviceCategoryRepository = serviceCategoryRepository;
    }

    /// <summary>
    /// Lấy tất cả danh mục dịch vụ
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ</returns>
    public async Task<IEnumerable<ServiceCategoryResponse>> GetAllAsync()
    {
        var categories = await _serviceCategoryRepository.GetAllAsync();
        return categories.Select(MapToResponse);
    }

    /// <summary>
    /// Lấy danh mục dịch vụ đang hoạt động (Public API)
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ đang hoạt động</returns>
    public async Task<IEnumerable<ServiceCategoryResponse>> GetActiveAsync()
    {
        var categories = await _serviceCategoryRepository.GetActiveAsync();
        return categories.Select(MapToResponse);
    }

    /// <summary>
    /// Lấy danh mục dịch vụ theo ID
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <returns>Thông tin danh mục</returns>
    public async Task<ServiceCategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _serviceCategoryRepository.GetByIdAsync(id);
        return category != null ? MapToResponse(category) : null;
    }

    /// <summary>
    /// Tạo danh mục dịch vụ mới
    /// </summary>
    /// <param name="request">Thông tin danh mục mới</param>
    /// <returns>Danh mục đã tạo</returns>
    public async Task<ServiceCategoryResponse> CreateAsync(CreateServiceCategoryRequest request)
    {
        // Kiểm tra tên danh mục đã tồn tại chưa
        if (await _serviceCategoryRepository.ExistsByNameAsync(request.CategoryName))
        {
            throw new ArgumentException("Tên danh mục đã tồn tại");
        }

        var serviceCategory = new ServiceCategory
        {
            CategoryName = request.CategoryName,
            Description = request.Description,
            IsActive = true, // Mặc định là active
            CreatedAt = DateTime.UtcNow
        };

        var createdCategory = await _serviceCategoryRepository.CreateAsync(serviceCategory);
        return MapToResponse(createdCategory);
    }

    /// <summary>
    /// Cập nhật danh mục dịch vụ
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <param name="request">Thông tin cập nhật</param>
    /// <returns>Danh mục đã cập nhật</returns>
    public async Task<ServiceCategoryResponse> UpdateAsync(int id, UpdateServiceCategoryRequest request)
    {
        var existingCategory = await _serviceCategoryRepository.GetByIdAsync(id);
        if (existingCategory == null)
        {
            throw new ArgumentException("Không tìm thấy danh mục dịch vụ");
        }

        // Kiểm tra tên danh mục đã tồn tại chưa (loại trừ danh mục hiện tại)
        if (await _serviceCategoryRepository.ExistsByNameAsync(request.CategoryName, id))
        {
            throw new ArgumentException("Tên danh mục đã tồn tại");
        }

        existingCategory.CategoryName = request.CategoryName;
        existingCategory.Description = request.Description;

        var updatedCategory = await _serviceCategoryRepository.UpdateAsync(existingCategory);
        return MapToResponse(updatedCategory);
    }

    /// <summary>
    /// Thay đổi trạng thái hoạt động của danh mục (Chỉ Admin)
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <param name="isActive">Trạng thái mới</param>
    /// <returns>True nếu thành công</returns>
    public async Task<bool> ToggleActiveAsync(int id, bool isActive)
    {
        var existingCategory = await _serviceCategoryRepository.GetByIdAsync(id);
        if (existingCategory == null)
        {
            throw new ArgumentException("Không tìm thấy danh mục dịch vụ");
        }

        return await _serviceCategoryRepository.ToggleActiveAsync(id, isActive);
    }

    /// <summary>
    /// Map entity sang response model
    /// </summary>
    /// <param name="category">Entity danh mục</param>
    /// <returns>Response model</returns>
    private static ServiceCategoryResponse MapToResponse(ServiceCategory category)
    {
        return new ServiceCategoryResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }
}
