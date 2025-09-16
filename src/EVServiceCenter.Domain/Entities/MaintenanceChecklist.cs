using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceChecklist
{
    public int ChecklistId { get; set; }

    public int WorkOrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Notes { get; set; }

    public virtual ICollection<MaintenanceChecklistResult> MaintenanceChecklistResults { get; set; } = new List<MaintenanceChecklistResult>();

    public virtual WorkOrder WorkOrder { get; set; }
}
