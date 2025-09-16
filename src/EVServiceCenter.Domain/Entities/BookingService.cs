using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class BookingService
{
    public int BookingId { get; set; }

    public int ServiceId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual Service Service { get; set; }
}
