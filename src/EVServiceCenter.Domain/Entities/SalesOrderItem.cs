using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class SalesOrderItem
{
    public int SalesOrderId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Part Part { get; set; }

    public virtual SalesOrder SalesOrder { get; set; }
}
