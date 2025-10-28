using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    public class InventoryUsageResponse
    {
        public List<PartUsageItem> HotParts { get; set; } = new List<PartUsageItem>();
        public List<PartUsageItem> NotHotParts { get; set; } = new List<PartUsageItem>();
        public List<PartUsageItem> UnusedParts { get; set; } = new List<PartUsageItem>();
        public InventoryUsageSummary Summary { get; set; } = new InventoryUsageSummary();
    }

    public class PartUsageItem
    {
        public int PartId { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public int UsageCount { get; set; }
        public decimal UsageValue { get; set; }
        public decimal UnitPrice { get; set; }
        public double UsageRate { get; set; } // Tỷ lệ sử dụng/tồn kho
        public string StockStatus { get; set; } = string.Empty; // NORMAL, LOW, OUT
        public DateTime LastUsedDate { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class InventoryUsageSummary
    {
        public int TotalParts { get; set; }
        public int HotPartsCount { get; set; }
        public int NotHotPartsCount { get; set; }
        public int UnusedPartsCount { get; set; }
        public int LowStockPartsCount { get; set; }
        public int OutOfStockPartsCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal TotalUsageValue { get; set; }
        public double AverageUsageRate { get; set; }
    }
}
