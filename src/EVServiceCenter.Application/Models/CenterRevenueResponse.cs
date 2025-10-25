using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models
{
    public class CenterRevenueResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CenterRevenueData>? Data { get; set; }
        public PaginationInfo? Pagination { get; set; }
        public SummaryInfo? Summary { get; set; }
    }

    public class CenterRevenueData
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal AverageBookingValue { get; set; }
        public List<RevenueByService>? RevenueByService { get; set; }
        public List<RevenueByMonth>? RevenueByMonth { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class RevenueByService
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class RevenueByMonth
    {
        public string Month { get; set; } = string.Empty; // Format: "2024-01"
        public string MonthName { get; set; } = string.Empty; // Format: "Tháng 1/2024"
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }

    public class SummaryInfo
    {
        public int TotalCenters { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageRevenuePerCenter { get; set; }
        public DateRangeInfo? DateRange { get; set; }
        public TopCenterInfo? TopPerformingCenter { get; set; }
    }

    public class TopCenterInfo
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }
}
