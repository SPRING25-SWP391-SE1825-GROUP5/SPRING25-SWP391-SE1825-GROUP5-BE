using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateServicePackageRequest
{
    [Required(ErrorMessage = "Tên gói dịch vụ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên gói dịch vụ không được vượt quá 100 ký tự")]
    public required string PackageName { get; set; }

    [Required(ErrorMessage = "Mã gói dịch vụ là bắt buộc")]
    [StringLength(50, ErrorMessage = "Mã gói dịch vụ không được vượt quá 50 ký tự")]
    public required string PackageCode { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
    public required int ServiceId { get; set; }

    [Required(ErrorMessage = "Tổng số credit là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Tổng số credit phải lớn hơn 0")]
    public required int TotalCredits { get; set; }

    [Required(ErrorMessage = "Giá gói là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá gói phải lớn hơn hoặc bằng 0")]
    public required decimal Price { get; set; }

    [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
    public decimal? DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}
