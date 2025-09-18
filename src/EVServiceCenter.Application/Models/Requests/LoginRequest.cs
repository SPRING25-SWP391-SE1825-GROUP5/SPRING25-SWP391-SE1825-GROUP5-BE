using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Models.Requests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Email phải có đuôi @gmail.com")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; }
        
        public string PasswordHash { get; set; }
    }
}
