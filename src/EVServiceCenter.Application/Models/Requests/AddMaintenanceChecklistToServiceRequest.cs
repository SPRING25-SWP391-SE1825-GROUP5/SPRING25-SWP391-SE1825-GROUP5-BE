using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddMaintenanceChecklistToServiceRequest
    {
        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải lớn hơn 0")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Danh sách mục kiểm tra là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 mục kiểm tra")]
        public List<ChecklistItemData> Items { get; set; } = new List<ChecklistItemData>();

        public class ChecklistItemData
        {
            [Required(ErrorMessage = "Tên mục kiểm tra là bắt buộc")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên mục kiểm tra phải có từ 2-200 ký tự")]
            public string ItemName { get; set; }

            [Required(ErrorMessage = "Mô tả là bắt buộc")]
            [StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải có từ 5-500 ký tự")]
            public string Description { get; set; }
        }
    }
}





