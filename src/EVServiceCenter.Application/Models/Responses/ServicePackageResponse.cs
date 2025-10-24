using System;

namespace EVServiceCenter.Application.Models.Responses;

public class ServicePackageResponse
{
    /// <summary>
    /// ID gói dịch vụ
    /// </summary>
    public int PackageId { get; set; }

    /// <summary>
    /// Tên gói dịch vụ
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// Mã gói dịch vụ
    /// </summary>
    public string PackageCode { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả gói dịch vụ
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ID dịch vụ
    /// </summary>
    public int ServiceId { get; set; }

    /// <summary>
    /// Tên dịch vụ
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Tổng số credit
    /// </summary>
    public int TotalCredits { get; set; }

    /// <summary>
    /// Giá gói dịch vụ
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Phần trăm giảm giá
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Ngày bắt đầu hiệu lực
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Ngày kết thúc hiệu lực
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Ngày cập nhật
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}