using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpdateVehicleModelPartRequest
{
    public bool? IsCompatible { get; set; }

    [StringLength(200, ErrorMessage = "Compatibility notes cannot exceed 200 characters")]
    public string? CompatibilityNotes { get; set; }
}
