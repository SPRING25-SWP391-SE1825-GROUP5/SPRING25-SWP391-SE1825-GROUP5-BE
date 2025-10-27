using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceChecklist
{
    public int ChecklistId { get; set; }

    public int BookingId { get; set; }

    public int TemplateId { get; set; }

    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<MaintenanceChecklistResult> MaintenanceChecklistResults { get; set; } = new List<MaintenanceChecklistResult>();

    public virtual Booking Booking { get; set; }
}
