using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class CustomerServiceCredit
{
    public int CreditId { get; set; }
    public int CustomerId { get; set; }
    public int PackageId { get; set; }
    public int ServiceId { get; set; }
    public int TotalCredits { get; set; }
    public int UsedCredits { get; set; } = 0;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed property (will be handled by EF)
    public int RemainingCredits => TotalCredits - UsedCredits;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ServicePackage ServicePackage { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
}
