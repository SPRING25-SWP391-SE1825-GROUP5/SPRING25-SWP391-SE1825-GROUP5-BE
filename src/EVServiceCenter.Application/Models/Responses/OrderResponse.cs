using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses;

public class OrderResponse
{
    public int OrderId { get; set; }
    public required string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public required string CustomerName { get; set; } = string.Empty;
    public required string CustomerPhone { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public required string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required List<OrderItemResponse> OrderItems { get; set; } = new List<OrderItemResponse>();
    // Removed: StatusHistory (history table dropped)

    // Gợi ý center fulfill gần nhất (không lưu DB, chỉ trả về lúc tạo đơn)
    public int? SuggestedFulfillmentCenterId { get; set; }
    public double? SuggestedFulfillmentDistanceKm { get; set; }

    // Chi nhánh đã fulfill order này (đã lưu trong DB sau khi thanh toán)
    public int? FulfillmentCenterId { get; set; }
    public string? FulfillmentCenterName { get; set; }
}

public class OrderItemResponse
{
    public int OrderItemId { get; set; }
    public int PartId { get; set; }
    public required string PartName { get; set; } = string.Empty;
    public required string PartNumber { get; set; } = string.Empty;
    public required string Brand { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

// Removed: OrderStatusHistoryResponse (history table dropped)
