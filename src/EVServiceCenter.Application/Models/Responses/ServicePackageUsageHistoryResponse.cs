using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ServicePackageUsageHistoryResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public int CreditsUsed { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
