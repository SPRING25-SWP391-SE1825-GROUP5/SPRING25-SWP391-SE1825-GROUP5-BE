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
        [Required(ErrorMessage = "Email ho?c s? di?n tho?i là b?t bu?c")]
        public required string EmailOrPhone { get; set; }

        [Required(ErrorMessage = "M?t kh?u là b?t bu?c")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "M?t kh?u không du?c d? tr?ng")]
        public required string Password { get; set; }
    }
}
