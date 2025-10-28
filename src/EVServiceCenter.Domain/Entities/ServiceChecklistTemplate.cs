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
    
    // Maintenance recommendation fields
    public int? MinKm { get; set; }           // Số km tối thiểu để áp dụng template này
    public int? MaxDate { get; set; }         // Số ngày tối đa (có thể là MaxKm hoặc MaxDays)
    public int? IntervalKm { get; set; }      // Khoảng cách km giữa các lần bảo dưỡng
    public int? IntervalDays { get; set; }    // Khoảng cách ngày giữa các lần bảo dưỡng
    public int? MaxOverdueDays { get; set; }  // Số ngày tối đa có thể trễ bảo dưỡng
    
    // Navigation properties
    public Service? Service { get; set; }
}


