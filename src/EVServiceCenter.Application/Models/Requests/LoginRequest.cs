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
       [Required(ErrorMessage = "Email hoặc số điện thoại là bắt buộc")]
        public string EmailOrPhone { get; set; }
        
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; }
        
    }
}
