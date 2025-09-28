using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    public string? Notes { get; set; }
}
