using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class QuickCreateAccountRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 số")]
        public string PhoneNumber { get; set; }

        [RegularExpression(@"^(ADMIN|STAFF|TECHNICIAN|CUSTOMER)$", ErrorMessage = "Vai trò phải là ADMIN, STAFF, TECHNICIAN hoặc CUSTOMER")]
        public string Role { get; set; } = "CUSTOMER";
    }
}


