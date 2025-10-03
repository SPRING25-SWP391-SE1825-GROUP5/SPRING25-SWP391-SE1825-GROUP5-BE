using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateVehicleModelPartRequest
{
    [Required(ErrorMessage = "Model ID is required")]
    public int ModelId { get; set; }

    [Required(ErrorMessage = "Part ID is required")]
    public int PartId { get; set; }

    public bool IsCompatible { get; set; } = true;

    [StringLength(200, ErrorMessage = "Compatibility notes cannot exceed 200 characters")]
    public string? CompatibilityNotes { get; set; }
}
