using System;

namespace EVServiceCenter.Application.Models.Responses;

public class VehicleModelResponse
{
    public int ModelId { get; set; }
    public string ModelName { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VehicleCount { get; set; }
    public int CompatiblePartsCount { get; set; }
}
