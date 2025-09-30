using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Requests;

public class QuickOrderRequest
{
	[JsonIgnore]
	public int CustomerId { get; set; }

	[Required]
	[MinLength(1, ErrorMessage = "Danh sách sản phẩm không được rỗng")]
	public List<QuickOrderItemRequest> Items { get; set; } = new();

	public string? Notes { get; set; }

	public string? ShippingAddress { get; set; }

	[MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
	[RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ (9-15 chữ số, có thể bắt đầu bằng +)")]
	public string? ShippingPhone { get; set; }
}
