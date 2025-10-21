using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class PartCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual PartCategory? Parent { get; set; }
    public virtual ICollection<PartCategory> Children { get; set; } = new List<PartCategory>();
    public virtual ICollection<PartCategoryMap> PartCategoryMaps { get; set; } = new List<PartCategoryMap>();
}
