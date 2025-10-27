using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class RevenueReportRequest
    {
        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; }

        public string Period { get; set; } = "daily"; // daily, weekly, monthly, quarterly

        public bool CompareWithPrevious { get; set; } = false;

        public string GroupBy { get; set; } = "none"; // service, technician, none
    }
}
