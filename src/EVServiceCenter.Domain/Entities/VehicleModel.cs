using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class VehicleModel
{
    public int ModelId { get; set; }

    public string ModelName { get; set; } = null!;

    // Removed specs: BatteryCapacity, MaxRange, MaxSpeed, ChargingTime, Weight, Price, Year

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? Version { get; set; }

    public string? ImageUrl { get; set; }

    // Navigation properties
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<VehicleModelPart> VehicleModelParts { get; set; } = new List<VehicleModelPart>();
}
