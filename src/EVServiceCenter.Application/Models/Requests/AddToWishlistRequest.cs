using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class AddToWishlistRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int PartId { get; set; }
}
