using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InventoryTransferItem
{
    public long TransferId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public virtual Part Part { get; set; }

    public virtual InventoryTransfer Transfer { get; set; }
}
