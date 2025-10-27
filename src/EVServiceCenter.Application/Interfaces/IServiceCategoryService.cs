using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces;

public interface IServiceCategoryService
{
    /// <summary>
    /// Lấy tất cả danh mục dịch vụ
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ</returns>
    Task<IEnumerable<ServiceCategoryResponse>> GetAllAsync();

    /// <summary>
    /// Lấy danh mục dịch vụ đang hoạt động (Public API)
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ đang hoạt động</returns>
    Task<IEnumerable<ServiceCategoryResponse>> GetActiveAsync();

    /// <summary>
    /// Lấy danh mục dịch vụ theo ID
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <returns>Thông tin danh mục</returns>
    Task<ServiceCategoryResponse?> GetByIdAsync(int id);

    /// <summary>
    /// Tạo danh mục dịch vụ mới
    /// </summary>
    /// <param name="request">Thông tin danh mục mới</param>
    /// <returns>Danh mục đã tạo</returns>
    Task<ServiceCategoryResponse> CreateAsync(CreateServiceCategoryRequest request);

    /// <summary>
    /// Cập nhật danh mục dịch vụ
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <param name="request">Thông tin cập nhật</param>
    /// <returns>Danh mục đã cập nhật</returns>
    Task<ServiceCategoryResponse> UpdateAsync(int id, UpdateServiceCategoryRequest request);

    /// <summary>
    /// Thay đổi trạng thái hoạt động của danh mục (Chỉ Admin)
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <param name="isActive">Trạng thái mới</param>
    /// <returns>True nếu thành công</returns>
    Task<bool> ToggleActiveAsync(int id, bool isActive);
}
