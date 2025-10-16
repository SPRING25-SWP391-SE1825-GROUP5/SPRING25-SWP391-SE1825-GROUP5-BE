using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google token là bắt buộc")]
        public required string Token { get; set; }
    }
}
