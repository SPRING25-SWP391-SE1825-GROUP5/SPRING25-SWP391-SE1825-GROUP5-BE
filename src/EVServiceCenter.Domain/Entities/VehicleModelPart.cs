using System;

namespace EVServiceCenter.Domain.Entities;

public partial class VehicleModelPart
{
    public int Id { get; set; }

    public int ModelId { get; set; }

    public int PartId { get; set; }

    public bool IsCompatible { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual VehicleModel VehicleModel { get; set; } = null!;

    public virtual Part Part { get; set; } = null!;
}
