using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InvoiceItem
{
    public int InvoiceItemId { get; set; }

    public int InvoiceId { get; set; }

    

    public string Description { get; set; }

    public virtual Invoice Invoice { get; set; }

    

    public int? OrderItemId { get; set; }

    public virtual OrderItem? OrderItem { get; set; }
}
