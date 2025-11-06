using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateOrderRequest
{
    [JsonIgnore]
    public int CustomerId { get; set; }

    public string? Notes { get; set; }

    // Optional: nếu gửi sẽ ghi vào đơn; nếu không gửi server tự suy ra/điền
    public string? ShippingAddress { get; set; }

    // Toạ độ để chọn center gần nhất có hàng (geocode/geoIP) - optional
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Không còn ShoppingCart: cho phép gửi items trực tiếp
    public List<QuickOrderItemRequest>? Items { get; set; }
}
