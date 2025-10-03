using System;

namespace EVServiceCenter.Application.Models.Responses;

public class VehicleModelPartResponse
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public int PartId { get; set; }
    public bool IsCompatible { get; set; }
    public string? CompatibilityNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Additional information
    public string? ModelName { get; set; }
    public string? Brand { get; set; }
    public string? PartName { get; set; }
    public string? PartNumber { get; set; }
    public string? PartBrand { get; set; }
    public decimal? PartUnitPrice { get; set; }
}
