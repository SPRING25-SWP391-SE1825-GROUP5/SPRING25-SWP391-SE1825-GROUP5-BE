using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddMaintenancePoliciesToServiceRequest
    {
        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải lớn hơn 0")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Danh sách chính sách là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 chính sách")]
        public List<MaintenancePolicyData> Policies { get; set; } = new List<MaintenancePolicyData>();

        public class MaintenancePolicyData
        {
            [Required(ErrorMessage = "Khoảng thời gian (tháng) là bắt buộc")]
            [Range(1, int.MaxValue, ErrorMessage = "Khoảng thời gian phải lớn hơn 0")]
            public int IntervalMonths { get; set; }

            [Required(ErrorMessage = "Khoảng km là bắt buộc")]
            [Range(1, int.MaxValue, ErrorMessage = "Khoảng km phải lớn hơn 0")]
            public int IntervalKm { get; set; }

            public bool IsActive { get; set; } = true;
        }
    }
}





