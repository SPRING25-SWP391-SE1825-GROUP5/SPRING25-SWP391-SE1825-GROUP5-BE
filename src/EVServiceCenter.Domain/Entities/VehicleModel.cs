using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class VehicleModel
{
    public int ModelId { get; set; }

    public string Brand { get; set; }

    public string ModelName { get; set; }

    public int Year { get; set; }

    public decimal? BatteryCapacity { get; set; }

    public int? Range { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
