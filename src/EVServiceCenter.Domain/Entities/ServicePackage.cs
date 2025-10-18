using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class ServicePackage
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = null!;
    public string PackageCode { get; set; } = null!;
    public string? Description { get; set; }
    public int ServiceId { get; set; }
    public int TotalCredits { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Service Service { get; set; } = null!;
    public virtual ICollection<CustomerServiceCredit> CustomerServiceCredits { get; set; } = new List<CustomerServiceCredit>();
}
