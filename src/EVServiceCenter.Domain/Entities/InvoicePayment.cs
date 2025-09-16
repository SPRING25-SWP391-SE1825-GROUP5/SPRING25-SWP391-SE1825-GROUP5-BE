using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InvoicePayment
{
    public int InvoiceId { get; set; }

    public int PaymentId { get; set; }

    public decimal AppliedAmount { get; set; }

    public virtual Invoice Invoice { get; set; }

    public virtual Payment Payment { get; set; }
}
