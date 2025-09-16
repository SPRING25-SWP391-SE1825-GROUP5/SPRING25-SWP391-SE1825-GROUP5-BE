using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InventoryTransfer
{
    public long TransferId { get; set; }

    public int FromWarehouseId { get; set; }

    public int ToWarehouseId { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PostedAt { get; set; }

    public string Note { get; set; }

    public virtual Warehouse FromWarehouse { get; set; }

    public virtual ICollection<InventoryTransferItem> InventoryTransferItems { get; set; } = new List<InventoryTransferItem>();

    public virtual Warehouse ToWarehouse { get; set; }
}
