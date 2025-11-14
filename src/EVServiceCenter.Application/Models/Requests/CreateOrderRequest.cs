using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateOrderRequest
{
    [JsonIgnore]
    public int CustomerId { get; set; }

    public string? Notes { get; set; }

    // Toạ độ để chọn center gần nhất có hàng (geocode/geoIP) - optional
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Center được chọn từ FE để fulfill order này - optional
    // Nếu có: validate và lưu vào Order, sử dụng khi thanh toán
    // Nếu không có: backend tự động chọn center có đủ stock (logic cũ)
    public int? FulfillmentCenterId { get; set; }

    // Không còn ShoppingCart: cho phép gửi items trực tiếp
    public List<QuickOrderItemRequest>? Items { get; set; }
}
