using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class AddToCartRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int PartId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
}
