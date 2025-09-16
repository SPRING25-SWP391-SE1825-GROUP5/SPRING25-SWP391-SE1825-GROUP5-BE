using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InventoryBalance
{
    public int PartId { get; set; }

    public int WarehouseId { get; set; }

    public int Quantity { get; set; }

    public virtual Part Part { get; set; }

    public virtual Warehouse Warehouse { get; set; }
}
