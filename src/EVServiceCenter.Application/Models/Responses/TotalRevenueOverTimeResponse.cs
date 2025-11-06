using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TotalRevenueOverTimeResponse
    {
        public bool Success { get; set; } = true;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Granularity { get; set; } = "DAY";
        public decimal TotalRevenue { get; set; }
        public List<TotalRevenuePeriod> Periods { get; set; } = new List<TotalRevenuePeriod>();
    }

    public class TotalRevenuePeriod
    {
        public string PeriodKey { get; set; } = string.Empty; // e.g., 2025-11-05, 2025-11, 2025-Q4, 2025
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Revenue { get; set; }
    }
}


