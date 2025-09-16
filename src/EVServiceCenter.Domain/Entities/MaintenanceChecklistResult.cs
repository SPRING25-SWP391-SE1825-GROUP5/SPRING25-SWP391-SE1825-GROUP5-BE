using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceChecklistResult
{
    public int ChecklistId { get; set; }

    public int ItemId { get; set; }

    public bool Performed { get; set; }

    public string Result { get; set; }

    public string Comment { get; set; }

    public virtual MaintenanceChecklist Checklist { get; set; }

    public virtual MaintenanceChecklistItem Item { get; set; }
}
