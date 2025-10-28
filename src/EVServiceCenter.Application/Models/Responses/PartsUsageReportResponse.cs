using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    public class PartsUsageReportResponse
    {
        public PartsUsageSummary Summary { get; set; } = new PartsUsageSummary();
        public List<PartUsageDetail> HotParts { get; set; } = new List<PartUsageDetail>();
        public List<PartUsageDetail> NotHotParts { get; set; } = new List<PartUsageDetail>();
        public List<PartUsageDetail> UnusedParts { get; set; } = new List<PartUsageDetail>();
        public PartsUsageTrends Trends { get; set; } = new PartsUsageTrends();
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PartsUsageComparison? Comparison { get; set; }
    }

    public class PartsUsageSummary
    {
        public int TotalPartsUsed { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalUsageCount { get; set; }
        public int HotPartsCount { get; set; }
        public int NotHotPartsCount { get; set; }
        public int UnusedPartsCount { get; set; }
    }

    public class PartUsageDetail
    {
        public int PartId { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal Revenue { get; set; }
        public double Frequency { get; set; } // Số lần sử dụng/tuần
        public double UsageRate { get; set; } // Tỷ lệ sử dụng/tồn kho (%)
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Trend { get; set; } // So với kỳ trước
    }

    public class PartsUsageTrends
    {
        public string RevenueGrowth { get; set; } = string.Empty;
        public string UsageGrowth { get; set; } = string.Empty;
        public int NewPartsAdded { get; set; }
    }

    public class PartsUsageComparison
    {
        public string PreviousPeriod { get; set; } = string.Empty;
        public decimal RevenueChange { get; set; }
        public int UsageChange { get; set; }
    }
}
