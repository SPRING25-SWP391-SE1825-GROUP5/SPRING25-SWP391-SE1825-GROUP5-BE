using System;

namespace EVServiceCenter.Application.Models.Responses;

public class CustomerServiceCreditResponse
{
    public required int CreditId { get; set; }
    public required int CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public required int PackageId { get; set; }
    public required string PackageName { get; set; }
    public required string PackageCode { get; set; }
    public required int ServiceId { get; set; }
    public required string ServiceName { get; set; }
    public required int TotalCredits { get; set; }
    public required int UsedCredits { get; set; }
    public required int RemainingCredits { get; set; }
    public required DateTime PurchaseDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public required string Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}
