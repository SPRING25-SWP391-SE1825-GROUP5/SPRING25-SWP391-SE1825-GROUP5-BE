using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateCustomerRequest
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 số")]
        public string PhoneNumber { get; set; }

        [StringLength(20, ErrorMessage = "Mã khách hàng không được vượt quá 20 ký tự")]
        public string CustomerCode { get; set; }

        public bool IsGuest { get; set; } = false;
    }
}
