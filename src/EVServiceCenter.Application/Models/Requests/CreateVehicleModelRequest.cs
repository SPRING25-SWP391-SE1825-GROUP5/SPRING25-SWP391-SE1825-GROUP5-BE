using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateVehicleModelRequest
{
    [Required(ErrorMessage = "Model name is required")]
    [StringLength(100, ErrorMessage = "Model name cannot exceed 100 characters")]
    public string ModelName { get; set; } = null!;

    [Required(ErrorMessage = "Brand is required")]
    [StringLength(50, ErrorMessage = "Brand cannot exceed 50 characters")]
    public string Brand { get; set; } = null!;
    // Spec fields removed
}
