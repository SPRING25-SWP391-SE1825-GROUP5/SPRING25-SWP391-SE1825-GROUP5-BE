using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CustomerServicePackageDetailResponse
    {
        public int CreditId { get; set; }
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string PackageDescription { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal FinalPrice { get; set; }
        public int TotalCredits { get; set; }
        public int UsedCredits { get; set; }
        public int RemainingCredits { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        // Service details
        public ServicePackageServiceInfo ServiceInfo { get; set; } = new();
        
        // Usage summary
        public ServicePackageUsageSummary UsageSummary { get; set; } = new();
    }

    public class ServicePackageServiceInfo
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public int EstimatedDuration { get; set; }
    }

    public class ServicePackageUsageSummary
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSavings { get; set; }
        public DateTime? LastUsedDate { get; set; }
    }
}
