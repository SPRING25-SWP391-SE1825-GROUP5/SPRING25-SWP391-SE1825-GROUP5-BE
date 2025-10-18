using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    // WorkOrderId removed - functionality merged into Booking

    public int? CustomerId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? BookingId { get; set; }

    // Link trực tiếp tới Order thay vì OrderItem
    public int? OrderId { get; set; }

    // public int? ParentInvoiceId { get; set; } // Column không tồn tại trong database

    public virtual Customer Customer { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // Removed navigation to UserPromotions to prevent EF from creating a shadow FK (InvoiceId)

    // WorkOrder navigation removed - functionality merged into Booking

    public virtual Booking Booking { get; set; }

    public virtual Order? Order { get; set; }

    
}
