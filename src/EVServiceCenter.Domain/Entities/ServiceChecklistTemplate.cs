using System;

namespace EVServiceCenter.Domain.Entities;

public class ServiceChecklistTemplate
{
    public int TemplateId { get; set; }
    public int ServiceId { get; set; }
    public string TemplateName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}


