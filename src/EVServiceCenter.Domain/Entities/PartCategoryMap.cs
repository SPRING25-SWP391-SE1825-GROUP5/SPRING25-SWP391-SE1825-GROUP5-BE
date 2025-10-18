using System;

namespace EVServiceCenter.Domain.Entities;

public partial class PartCategoryMap
{
    public int PartId { get; set; }
    public int CategoryId { get; set; }
    public bool IsPrimary { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Part Part { get; set; } = null!;
    public virtual PartCategory Category { get; set; } = null!;
}
