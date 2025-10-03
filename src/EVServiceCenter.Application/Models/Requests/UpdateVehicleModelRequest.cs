using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpdateVehicleModelRequest
{
    [StringLength(100, ErrorMessage = "Model name cannot exceed 100 characters")]
    public string? ModelName { get; set; }

    [StringLength(50, ErrorMessage = "Brand cannot exceed 50 characters")]
    public string? Brand { get; set; }

    // Spec fields removed

    public bool? IsActive { get; set; }
}
