using System;

namespace EVServiceCenter.Domain.Entities;

public class ServiceChecklistTemplate
{
    public int TemplateID { get; set; }
    public int ServiceID { get; set; }
    public string TemplateName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Maintenance recommendation fields (only fields that exist in database)
    public int? MinKm { get; set; }
    public int? MaxDate { get; set; }
    public int? MaxOverdueDays { get; set; }
    
    // Navigation property
    public Service? Service { get; set; }
}


