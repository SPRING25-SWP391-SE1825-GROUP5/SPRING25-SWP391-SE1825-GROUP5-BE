using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public int CenterId { get; set; }

    public int PartId { get; set; }

    public int CurrentStock { get; set; }

    public int MinimumStock { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual Part Part { get; set; }
}
