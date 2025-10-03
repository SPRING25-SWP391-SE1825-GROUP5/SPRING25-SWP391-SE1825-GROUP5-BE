using System;

namespace EVServiceCenter.Application.Models.Responses;

public class VehicleModelResponse
{
    public int ModelId { get; set; }
    public string ModelName { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public decimal? BatteryCapacity { get; set; }
    public int? MaxRange { get; set; }
    public int? MaxSpeed { get; set; }
    public int? ChargingTime { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Price { get; set; }
    public int? Year { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int VehicleCount { get; set; }
    public int CompatiblePartsCount { get; set; }
}
