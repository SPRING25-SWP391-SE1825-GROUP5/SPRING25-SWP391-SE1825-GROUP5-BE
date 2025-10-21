using System;

namespace EVServiceCenter.Application.Models.Responses;

public class ServicePackageResponse
{
    public required int PackageId { get; set; }
    public required string PackageName { get; set; }
    public required string PackageCode { get; set; }
    public string? Description { get; set; }
    public required int ServiceId { get; set; }
    public required string ServiceName { get; set; }
    public required int TotalCredits { get; set; }
    public required decimal Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public required bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}
