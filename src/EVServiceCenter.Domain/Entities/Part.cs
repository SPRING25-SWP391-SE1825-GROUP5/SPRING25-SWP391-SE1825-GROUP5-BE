using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Part
{
    public int PartId { get; set; }

    public string PartNumber { get; set; }

    public string PartName { get; set; }

    public string Brand { get; set; }

    public decimal UnitPrice { get; set; }

    public string Unit { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<InventoryTransferItem> InventoryTransferItems { get; set; } = new List<InventoryTransferItem>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();

    public virtual ICollection<WorkOrderChargeProposalItem> WorkOrderChargeProposalItems { get; set; } = new List<WorkOrderChargeProposalItem>();

    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();
}
