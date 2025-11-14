using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Part
{
    public int PartId { get; set; }

    public string PartNumber { get; set; }

    public string PartName { get; set; }

    public string? Brand { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal? Rating { get; set; }

    public virtual ICollection<InventoryPart> InventoryParts { get; set; } = new List<InventoryPart>();





    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();

    // E-commerce navigation properties


    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();





    public virtual ICollection<VehicleModelPart> VehicleModelParts { get; set; } = new List<VehicleModelPart>();
}
