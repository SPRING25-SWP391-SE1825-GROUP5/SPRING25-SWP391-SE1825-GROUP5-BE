using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CustomerServicePackageResponse
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
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
    }
}
