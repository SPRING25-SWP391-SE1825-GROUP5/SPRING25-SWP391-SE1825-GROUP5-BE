using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ServiceBookingStatsResponse
    {
        public bool Success { get; set; } = true;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalCompletedBookings { get; set; }
        public decimal TotalServiceRevenue { get; set; }
        public List<ServiceBookingStatsItem> Services { get; set; } = new();
    }

    public class ServiceBookingStatsItem
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal ServiceRevenue { get; set; }
    }
}


