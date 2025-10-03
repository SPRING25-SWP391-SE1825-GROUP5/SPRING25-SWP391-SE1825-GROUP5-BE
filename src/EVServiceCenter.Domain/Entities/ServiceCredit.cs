using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class ServiceCredit
{
    public int CreditId { get; set; }

    public int CustomerId { get; set; }

    public int ServiceId { get; set; }

    public int QtyPurchased { get; set; }

    public int QtyRemaining { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    public decimal PriceDiscount { get; set; }

    // Removed InvoiceId: credit không còn gắn trực tiếp invoice

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Service Service { get; set; }

    // Removed navigation to Invoice
}
