using System;

namespace EVServiceCenter.Application.Models.Responses;

public class VehicleModelResponse
{
    public int ModelId { get; set; }
    public required string ModelName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VehicleCount { get; set; }
    public int CompatiblePartsCount { get; set; }
    public string? Version { get; set; }
}
