using EVServiceCenter.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVServiceCenter.Domain.Interfaces;

public interface IServiceCategoryRepository
{
    /// <summary>
    /// Lấy tất cả danh mục dịch vụ
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ</returns>
    Task<IEnumerable<ServiceCategory>> GetAllAsync();

    /// <summary>
    /// Lấy danh mục dịch vụ đang hoạt động
    /// </summary>
    /// <returns>Danh sách danh mục dịch vụ đang hoạt động</returns>
    Task<IEnumerable<ServiceCategory>> GetActiveAsync();

    /// <summary>
    /// Lấy danh mục dịch vụ theo ID
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <returns>Danh mục dịch vụ</returns>
    Task<ServiceCategory?> GetByIdAsync(int id);

    /// <summary>
    /// Tạo danh mục dịch vụ mới
    /// </summary>
    /// <param name="serviceCategory">Thông tin danh mục mới</param>
    /// <returns>Danh mục đã tạo</returns>
    Task<ServiceCategory> CreateAsync(ServiceCategory serviceCategory);

    /// <summary>
    /// Cập nhật danh mục dịch vụ
    /// </summary>
    /// <param name="serviceCategory">Thông tin danh mục cần cập nhật</param>
    /// <returns>Danh mục đã cập nhật</returns>
    Task<ServiceCategory> UpdateAsync(ServiceCategory serviceCategory);

    /// <summary>
    /// Thay đổi trạng thái hoạt động của danh mục
    /// </summary>
    /// <param name="id">ID danh mục</param>
    /// <param name="isActive">Trạng thái mới</param>
    /// <returns>True nếu thành công</returns>
    Task<bool> ToggleActiveAsync(int id, bool isActive);

    /// <summary>
    /// Kiểm tra tên danh mục có tồn tại không
    /// </summary>
    /// <param name="categoryName">Tên danh mục</param>
    /// <param name="excludeId">ID danh mục cần loại trừ (dùng khi cập nhật)</param>
    /// <returns>True nếu tên đã tồn tại</returns>
    Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null);
}
