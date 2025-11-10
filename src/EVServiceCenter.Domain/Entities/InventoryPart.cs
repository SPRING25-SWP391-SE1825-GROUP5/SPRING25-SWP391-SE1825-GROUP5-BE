using System;

namespace EVServiceCenter.Domain.Entities;

public partial class InventoryPart
{
    public int InventoryPartId { get; set; }
    public int InventoryId { get; set; }
    public int PartId { get; set; }
    public int CurrentStock { get; set; }
    public int ReservedQty { get; set; } // Số lượng đã reserve (chưa trừ thực tế)
    public int MinimumStock { get; set; }
    public DateTime LastUpdated { get; set; }

    public virtual Inventory Inventory { get; set; }
    public virtual Part Part { get; set; }

    // Computed property: Số lượng có thể bán (CurrentStock - ReservedQty)
    public int AvailableQty => CurrentStock - ReservedQty;
}
