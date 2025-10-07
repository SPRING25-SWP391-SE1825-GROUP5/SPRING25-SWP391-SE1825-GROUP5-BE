using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateMaintenanceChecklistItemRequest
    {
        [Required(ErrorMessage = "Tên mục kiểm tra là bắt buộc")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên mục kiểm tra phải có từ 2-200 ký tự")]
        public string ItemName { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải có từ 5-500 ký tự")]
        public string Description { get; set; }
    }
}

