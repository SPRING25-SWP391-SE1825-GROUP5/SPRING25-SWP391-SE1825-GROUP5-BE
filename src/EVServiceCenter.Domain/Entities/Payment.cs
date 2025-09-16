using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public string PaymentCode { get; set; }

    public int InvoiceId { get; set; }

    public long PayOsorderCode { get; set; }

    public int Amount { get; set; }

    public string Status { get; set; }

    public string BuyerName { get; set; }

    public string BuyerPhone { get; set; }

    public string BuyerAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Invoice Invoice { get; set; }

    public virtual ICollection<InvoicePayment> InvoicePayments { get; set; } = new List<InvoicePayment>();
}
