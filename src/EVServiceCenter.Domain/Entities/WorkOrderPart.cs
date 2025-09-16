using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrderPart
{
    public int WorkOrderId { get; set; }

    public int PartId { get; set; }

    public int QuantityUsed { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TotalCost { get; set; }

    public virtual Part Part { get; set; }

    public virtual WorkOrder WorkOrder { get; set; }
}
