using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceChecklistItem
{
    public int ItemId { get; set; }

    public string ItemName { get; set; }

    public string Description { get; set; }
}
