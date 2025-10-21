using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Inventory
{
    public int InventoryId { get; set; }
    public int CenterId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public virtual ServiceCenter Center { get; set; }
    public virtual ICollection<InventoryPart> InventoryParts { get; set; } = new List<InventoryPart>();
}
