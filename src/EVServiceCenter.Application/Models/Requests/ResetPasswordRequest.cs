using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EVServiceCenter.Application.Validations.ValidEmail]
        public required string Email { get; set; }
    }
}
