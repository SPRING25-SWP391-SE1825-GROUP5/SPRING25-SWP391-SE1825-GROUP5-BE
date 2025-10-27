using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CustomerServicePackageStatisticsResponse
    {
        public int TotalPackages { get; set; }
        public int ActivePackages { get; set; }
        public int ExpiredPackages { get; set; }
        public int UsedUpPackages { get; set; }
        public int TotalCreditsPurchased { get; set; }
        public int TotalCreditsUsed { get; set; }
        public int TotalCreditsRemaining { get; set; }
        public decimal TotalAmountSpent { get; set; }
        public decimal TotalSavings { get; set; }
        public DateTime? FirstPurchaseDate { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public DateTime? LastUsageDate { get; set; }
        
        // Top services
        public List<ServiceUsageStatistic> TopServices { get; set; } = new();
        
        // Monthly statistics
        public List<MonthlyStatistic> MonthlyStats { get; set; } = new();
    }

    public class ServiceUsageStatistic
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalSavings { get; set; }
    }

    public class MonthlyStatistic
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int PackagesPurchased { get; set; }
        public int CreditsUsed { get; set; }
        public decimal AmountSpent { get; set; }
        public decimal Savings { get; set; }
    }
}
