using System;
using System.ComponentModel.DataAnnotations;
using EVServiceCenter.Application.Validations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 số")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [MinimumAge(16, ErrorMessage = "Phải đủ 16 tuổi trở lên")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [RegularExpression(@"^(MALE|FEMALE)$", ErrorMessage = "Giới tính phải là MALE hoặc FEMALE")]
        public required string Gender { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public required string Address { get; set; }

        [RegularExpression(@"^(ADMIN|MANAGER|STAFF|TECHNICIAN|CUSTOMER)$", ErrorMessage = "Vai trò phải là ADMIN, MANAGER, STAFF, TECHNICIAN hoặc CUSTOMER")]
        public required string Role { get; set; } = "CUSTOMER";

        public bool IsActive { get; set; } = true;
        public bool EmailVerified { get; set; } = false;
    }
}
