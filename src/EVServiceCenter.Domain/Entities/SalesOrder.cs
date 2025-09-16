using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class SalesOrder
{
    public int SalesOrderId { get; set; }

    public int? CustomerId { get; set; }

    public int CenterId { get; set; }

    public int ChannelId { get; set; }

    public int WarehouseId { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual Channel Channel { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();

    public virtual Warehouse Warehouse { get; set; }
}
