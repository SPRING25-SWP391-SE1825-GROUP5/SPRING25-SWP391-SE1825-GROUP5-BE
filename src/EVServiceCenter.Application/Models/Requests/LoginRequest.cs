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
        [Required(ErrorMessage = "Email ho?c s? di?n tho?i l� b?t bu?c")]
        public required string EmailOrPhone { get; set; }

        [Required(ErrorMessage = "M?t kh?u l� b?t bu?c")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "M?t kh?u kh�ng du?c d? tr?ng")]
        public required string Password { get; set; }
    }
}
