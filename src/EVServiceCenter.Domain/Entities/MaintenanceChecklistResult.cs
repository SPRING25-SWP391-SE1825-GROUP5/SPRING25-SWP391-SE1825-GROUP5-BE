using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceChecklistResult
{
    public int ResultId { get; set; }

    public int ChecklistId { get; set; }

    public int? PartId { get; set; }

    public string? Description { get; set; }
    public bool IsMandatory { get; set; }

    public bool Performed { get; set; }

    public string? Result { get; set; }

    public string? Comment { get; set; }

    public virtual MaintenanceChecklist Checklist { get; set; }

    public virtual Part Part { get; set; }
}
