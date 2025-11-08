using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string Status { get; set; }

    public string? Notes { get; set; }



    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // PayOS orderCode - unique random number để tránh conflict với Booking
    public int? PayOSOrderCode { get; set; }

    // Fulfillment center ID - center nào đã fulfill order này (trừ kho)
    public int? FulfillmentCenterId { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Removed: OrderStatusHistories (table dropped)

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    // Navigation property cho FulfillmentCenter
    public virtual ServiceCenter? FulfillmentCenter { get; set; }
}
