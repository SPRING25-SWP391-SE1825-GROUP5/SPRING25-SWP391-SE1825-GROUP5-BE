using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpdateServicePackageRequest
{
    [StringLength(100, ErrorMessage = "Tên gói dịch vụ không được vượt quá 100 ký tự")]
    public string? PackageName { get; set; }

    [StringLength(50, ErrorMessage = "Mã gói dịch vụ không được vượt quá 50 ký tự")]
    public string? PackageCode { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải lớn hơn 0")]
    public int? ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Tổng số credit phải lớn hơn 0")]
    public int? TotalCredits { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá gói phải lớn hơn hoặc bằng 0")]
    public decimal? Price { get; set; }

    [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
    public decimal? DiscountPercent { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}
