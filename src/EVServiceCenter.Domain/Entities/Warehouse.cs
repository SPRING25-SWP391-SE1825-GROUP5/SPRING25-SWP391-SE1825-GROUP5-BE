using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public int CenterId { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public bool IsActive { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<InventoryTransfer> InventoryTransferFromWarehouses { get; set; } = new List<InventoryTransfer>();

    public virtual ICollection<InventoryTransfer> InventoryTransferToWarehouses { get; set; } = new List<InventoryTransfer>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
