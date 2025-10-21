using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class OrderHistoryResponse
    {
        public int OrderId { get; set; }
        public required string OrderNumber { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public required string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        
        public ShippingAddressInfo ShippingAddress { get; set; } = null!;
        public required List<OrderItemInfo> Items { get; set; } = new List<OrderItemInfo>();
        public OrderPaymentInfo? PaymentInfo { get; set; }
        public ShippingInfo? ShippingInfo { get; set; }
        public required List<OrderStatusTimelineInfo> Timeline { get; set; } = new List<OrderStatusTimelineInfo>();
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ShippingAddressInfo
    {
        public required string FullName { get; set; } = null!;
        public required string PhoneNumber { get; set; } = null!;
        public required string Address { get; set; } = null!;
    }

    public class OrderItemInfo
    {
        public int OrderItemId { get; set; }
        public int PartId { get; set; }
        public required string ProductName { get; set; } = null!;
        public string? PartNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderPaymentInfo
    {
        public int PaymentId { get; set; }
        public required string PaymentMethod { get; set; } = null!;
        public required string PaymentStatus { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public decimal Amount { get; set; }
    }

    public class ShippingInfo
    {
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? ShippingMethod { get; set; }
    }

    public class OrderStatusTimelineInfo
    {
        public required string Status { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string? Note { get; set; }
    }
}
