using System;

namespace EVServiceCenter.Domain.Entities;

public class ServiceChecklistTemplateItem
{
    public int ItemId { get; set; }
    public int TemplateId { get; set; }
    public int PartId { get; set; }
    public DateTime CreatedAt { get; set; }
}


