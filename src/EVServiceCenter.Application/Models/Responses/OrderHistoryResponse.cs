using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class OrderHistoryResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        
        public ShippingAddressInfo ShippingAddress { get; set; } = null!;
        public List<OrderItemInfo> Items { get; set; } = new List<OrderItemInfo>();
        public OrderPaymentInfo? PaymentInfo { get; set; }
        public ShippingInfo? ShippingInfo { get; set; }
        public List<OrderStatusTimelineInfo> Timeline { get; set; } = new List<OrderStatusTimelineInfo>();
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ShippingAddressInfo
    {
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Address { get; set; } = null!;
    }

    public class OrderItemInfo
    {
        public int OrderItemId { get; set; }
        public int PartId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? PartNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderPaymentInfo
    {
        public int PaymentId { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
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
        public string Status { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string? Note { get; set; }
    }
}
