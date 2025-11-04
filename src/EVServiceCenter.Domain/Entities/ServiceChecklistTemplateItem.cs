using System;

namespace EVServiceCenter.Domain.Entities;

public class ServiceChecklistTemplateItem
{
    public int ItemID { get; set; }
    public int TemplateID { get; set; }
    public int? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual PartCategory? Category { get; set; }
}


