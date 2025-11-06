using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpdateVehicleModelRequest
{
    [StringLength(100, ErrorMessage = "Model name cannot exceed 100 characters")]
    public string? ModelName { get; set; }

    [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Image URL must be a valid URL")]
    public string? ImageUrl { get; set; }

    public bool? IsActive { get; set; }
}
