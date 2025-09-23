using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateServiceRequest
    {
        [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên dịch vụ phải có từ 2-200 ký tự")]
        public string ServiceName { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả dịch vụ không được vượt quá 1000 ký tự")]
        public string Description { get; set; }

        [Required(ErrorMessage = "ID danh mục dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID danh mục dịch vụ phải lớn hơn 0")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Giá dịch vụ là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        // Với mô hình mỗi booking 1 slot cố định, không yêu cầu thời gian ước tính

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
