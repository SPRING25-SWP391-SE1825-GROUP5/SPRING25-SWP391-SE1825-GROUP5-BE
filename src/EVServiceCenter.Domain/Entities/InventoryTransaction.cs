using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    public int PartId { get; set; }

    public int WarehouseId { get; set; }

    public int QtyChange { get; set; }

    public string RefType { get; set; }

    public long? RefId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Part Part { get; set; }

    public virtual Warehouse Warehouse { get; set; }
}
