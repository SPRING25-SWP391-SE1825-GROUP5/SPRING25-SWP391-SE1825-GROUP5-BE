using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class InvoiceItem
{
    public int InvoiceItemId { get; set; }

    public int InvoiceId { get; set; }

    public int? PartId { get; set; }

    public string Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public virtual Invoice Invoice { get; set; }

    public virtual Part Part { get; set; }
}
