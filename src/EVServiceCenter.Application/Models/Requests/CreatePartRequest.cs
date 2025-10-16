using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreatePartRequest
    {
        [Required(ErrorMessage = "Mã phụ tùng là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Mã phụ tùng phải từ 3 đến 50 ký tự")]
        public required string PartNumber { get; set; }

        [Required(ErrorMessage = "Tên phụ tùng là bắt buộc")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên phụ tùng phải từ 5 đến 200 ký tự")]
        public required string PartName { get; set; }

        [Required(ErrorMessage = "Thương hiệu là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Thương hiệu phải từ 2 đến 100 ký tự")]
        public required string Brand { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải là số lớn hơn 0")]
        public decimal UnitPrice { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Trạng thái hoạt động là bắt buộc")]
        public bool IsActive { get; set; }
    }
}
