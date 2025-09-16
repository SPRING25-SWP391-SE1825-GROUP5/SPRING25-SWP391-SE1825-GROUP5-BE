using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class UserPromotion
{
    public int UserPromotionId { get; set; }

    public int CustomerId { get; set; }

    public int PromotionId { get; set; }

    public int? InvoiceId { get; set; }

    public DateTime UsedAt { get; set; }

    public decimal DiscountAmount { get; set; }

    public string Status { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Invoice Invoice { get; set; }

    public virtual Promotion Promotion { get; set; }
}
