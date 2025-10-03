using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class UserPromotion
{
    public int UserPromotionId { get; set; }

    public int CustomerId { get; set; }

    public int PromotionId { get; set; }

    public int? BookingId { get; set; }

    public int? OrderId { get; set; }

    public DateTime UsedAt { get; set; }

    public decimal DiscountAmount { get; set; }

    public string Status { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual Order Order { get; set; }

    public virtual Promotion Promotion { get; set; }
}
