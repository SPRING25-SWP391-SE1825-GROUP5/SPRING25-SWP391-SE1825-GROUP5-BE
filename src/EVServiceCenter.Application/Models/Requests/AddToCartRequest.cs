using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Requests;

public class AddToCartRequest
{
    [JsonIgnore]
    public int CustomerId { get; set; }

    [Required]
    public int PartId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
}
