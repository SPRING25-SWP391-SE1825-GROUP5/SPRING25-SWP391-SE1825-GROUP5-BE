using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string OrderNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
}
