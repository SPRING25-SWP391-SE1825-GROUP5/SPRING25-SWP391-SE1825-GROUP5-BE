using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateOrderRequest
{
    [JsonIgnore]
    public int CustomerId { get; set; }

    public string? Notes { get; set; }

    // Optional: nếu gửi sẽ ghi vào đơn; nếu không gửi server tự suy ra/điền
    public string? ShippingAddress { get; set; }

    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ (9-15 chữ số, có thể bắt đầu bằng +)")]
    public string? ShippingPhone { get; set; }
}
