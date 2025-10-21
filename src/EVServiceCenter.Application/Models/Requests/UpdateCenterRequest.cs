using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateCenterRequest
    {
        [Required(ErrorMessage = "Tên trung tâm là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên trung tâm phải từ 2-100 ký tự")]
        public required string CenterName { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10-255 ký tự")]
        public required string Address { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 số")]
        public required string PhoneNumber { get; set; }

        public bool IsActive { get; set; }
    }
}
