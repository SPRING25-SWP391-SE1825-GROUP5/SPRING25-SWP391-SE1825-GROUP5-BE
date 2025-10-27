using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models
{
    public class BookingStatisticsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public BookingStatisticsData? Data { get; set; }
    }

    public class BookingStatisticsData
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int InProgressBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public decimal CancelledRevenue { get; set; }
        public List<BookingStatusCount> StatusCounts { get; set; } = new();
        public List<MonthlyStatistics> MonthlyStats { get; set; } = new();
        public List<DailyStatistics> DailyStats { get; set; } = new();
        public List<ServiceTypeStatistics> ServiceTypeStats { get; set; } = new();
    }

    public class BookingStatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlyStatistics
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailyStatistics
    {
        public DateTime Date { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ServiceTypeStatistics
    {
        public string ServiceType { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
    }
}
