using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class ApplyPackageRequest
    {
        [Required(ErrorMessage = "Mã gói dịch vụ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã gói dịch vụ không được vượt quá 50 ký tự")]
        public string PackageCode { get; set; } = string.Empty;
    }
}
