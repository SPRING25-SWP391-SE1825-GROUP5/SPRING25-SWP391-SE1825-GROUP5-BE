using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    public class RevenueReportResponse
    {
        public RevenueSummary Summary { get; set; } = new RevenueSummary();
        public List<RevenueByPeriod> RevenueByPeriod { get; set; } = new List<RevenueByPeriod>();
        public GroupedRevenueData GroupedData { get; set; } = new GroupedRevenueData();
        public List<RevenueAlert> Alerts { get; set; } = new List<RevenueAlert>();
        public RevenueTrends Trends { get; set; } = new RevenueTrends();
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RevenueComparison? Comparison { get; set; }
    }

    public class RevenueSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageRevenuePerBooking { get; set; }
        public string GrowthRate { get; set; } = string.Empty;
        public string AlertLevel { get; set; } = "normal"; // normal, warning, critical
    }

    public class RevenueByPeriod
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
        public decimal Services { get; set; }
        public decimal Parts { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RevenueComparison? Comparison { get; set; }
    }

    public class GroupedRevenueData
    {
        public List<ServiceRevenue> ByService { get; set; } = new List<ServiceRevenue>();
        public List<TechnicianRevenue> ByTechnician { get; set; } = new List<TechnicianRevenue>();
    }

    public class ServiceRevenue
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
        public double Percentage { get; set; }
    }

    public class TechnicianRevenue
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
        public double AverageRating { get; set; }
    }

    public class RevenueAlert
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // warning, critical
    }

    public class RevenueTrends
    {
        public string Direction { get; set; } = string.Empty; // increasing, decreasing, stable
        public string Volatility { get; set; } = string.Empty; // low, medium, high
        public string PeakDay { get; set; } = string.Empty;
        public string LowestDay { get; set; } = string.Empty;
    }

    public class RevenueComparison
    {
        public string PreviousPeriod { get; set; } = string.Empty;
        public decimal RevenueChange { get; set; }
        public string PercentageChange { get; set; } = string.Empty;
        public int BookingChange { get; set; }
    }
}
