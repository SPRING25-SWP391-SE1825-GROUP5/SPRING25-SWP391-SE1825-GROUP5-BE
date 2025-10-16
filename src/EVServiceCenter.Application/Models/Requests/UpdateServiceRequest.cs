using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateServiceRequest
    {
        [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên dịch vụ phải có từ 2-200 ký tự")]
        public required string ServiceName { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả dịch vụ không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá dịch vụ là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ phải lớn hơn hoặc bằng 0")]
        public decimal BasePrice { get; set; }

        // Bỏ yêu cầu thời gian ước tính khi mỗi booking cố định 1 slot

        // Loại bỏ notes khỏi request vì DB không có cột tương ứng

        public bool IsActive { get; set; }
    }
}
