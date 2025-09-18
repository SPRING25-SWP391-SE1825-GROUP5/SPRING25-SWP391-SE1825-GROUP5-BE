using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateCenterRequest
    {
        [Required(ErrorMessage = "Tên trung tâm là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên trung tâm phải từ 2-100 ký tự")]
        public string CenterName { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10-255 ký tự")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Thành phố là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Thành phố phải từ 2-50 ký tự")]
        public string City { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 số")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        public bool IsActive { get; set; }
    }
}
