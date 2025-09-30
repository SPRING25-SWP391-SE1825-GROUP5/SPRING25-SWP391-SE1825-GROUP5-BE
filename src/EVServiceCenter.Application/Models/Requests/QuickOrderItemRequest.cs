using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class QuickOrderItemRequest
{
	[Required]
	public int PartId { get; set; }

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
	public int Quantity { get; set; }
}
