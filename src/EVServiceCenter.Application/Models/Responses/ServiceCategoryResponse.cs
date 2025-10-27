using System;

namespace EVServiceCenter.Application.Models.Responses;

public class ServiceCategoryResponse
{
    /// <summary>
    /// ID danh mục
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Tên danh mục
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả danh mục
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
