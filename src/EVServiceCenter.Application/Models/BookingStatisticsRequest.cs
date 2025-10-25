using System;

namespace EVServiceCenter.Application.Models
{
    public class BookingStatisticsRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CenterId { get; set; }
        public string? ServiceType { get; set; }
        public string? Status { get; set; }
        public bool IncludeMonthlyStats { get; set; } = true;
        public bool IncludeDailyStats { get; set; } = false;
        public bool IncludeServiceTypeStats { get; set; } = true;
    }

    public class CenterBookingStatisticsRequest
    {
        public int CenterId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ServiceType { get; set; }
        public string? Status { get; set; }
        public bool IncludeMonthlyStats { get; set; } = true;
        public bool IncludeDailyStats { get; set; } = false;
        public bool IncludeServiceTypeStats { get; set; } = true;
    }
}
