using System;
using System.ComponentModel.DataAnnotations;
using EVServiceCenter.Application.Validations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2-100 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [MinimumAge(16, ErrorMessage = "Phải đủ 16 tuổi trở lên")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [RegularExpression(@"^(MALE|FEMALE)$", ErrorMessage = "Giới tính phải là MALE hoặc FEMALE")]
        public string Gender { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string Address { get; set; }
    }
}
