using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateServiceCategoryRequest
{
    /// <summary>
    /// Tên danh mục dịch vụ
    /// </summary>
    [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên danh mục phải từ 2-100 ký tự")]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ0-9\s\-_]+$", ErrorMessage = "Tên danh mục chỉ được chứa chữ cái, số, khoảng trắng, dấu gạch ngang và gạch dưới")]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả danh mục
    /// </summary>
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }
}
