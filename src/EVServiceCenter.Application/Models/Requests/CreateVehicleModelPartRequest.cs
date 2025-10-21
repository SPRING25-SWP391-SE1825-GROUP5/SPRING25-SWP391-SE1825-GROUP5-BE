using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateVehicleModelPartRequest
{
    [Required(ErrorMessage = "Model ID is required")]
    public int ModelId { get; set; }

    [Required(ErrorMessage = "Part ID is required")]
    public int PartId { get; set; }

    // IsCompatible removed
}
