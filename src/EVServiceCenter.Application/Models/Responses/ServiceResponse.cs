using System;

namespace EVServiceCenter.Application.Models.Responses;

public class ServiceResponse
{
    /// <summary>
    /// ID dịch vụ
    /// </summary>
    public int ServiceId { get; set; }

    /// <summary>
    /// Tên dịch vụ
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả dịch vụ
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Giá cơ bản
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// ID danh mục
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Tên danh mục
    /// </summary>
    public string? CategoryName { get; set; }
}