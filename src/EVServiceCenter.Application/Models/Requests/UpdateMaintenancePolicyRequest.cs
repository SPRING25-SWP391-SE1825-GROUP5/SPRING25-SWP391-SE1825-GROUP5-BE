using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateMaintenancePolicyRequest
    {
        [Required(ErrorMessage = "Khoảng thời gian (tháng) là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Khoảng thời gian phải lớn hơn 0")]
        public int IntervalMonths { get; set; }

        [Required(ErrorMessage = "Khoảng km là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Khoảng km phải lớn hơn 0")]
        public int IntervalKm { get; set; }

        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải lớn hơn 0")]
        public int ServiceId { get; set; }

        public bool IsActive { get; set; }
    }
}

