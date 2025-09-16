using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; }

    public int WorkOrderId { get; set; }

    public int? CustomerId { get; set; }

    public string BillingName { get; set; }

    public string BillingPhone { get; set; }

    public string BillingAddress { get; set; }

    public string Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string NormalizedBillingPhone { get; set; }

    public string InvoiceType { get; set; }

    public int? ParentInvoiceId { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<InvoicePayment> InvoicePayments { get; set; } = new List<InvoicePayment>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();

    public virtual WorkOrder WorkOrder { get; set; }
}
