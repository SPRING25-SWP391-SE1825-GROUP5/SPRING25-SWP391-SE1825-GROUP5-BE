using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class BookingTimeSlot
{
    public int BookingId { get; set; }

    public int SlotId { get; set; }

    public int TechnicianId { get; set; }

    public int SlotOrder { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual TimeSlot Slot { get; set; }

    public virtual Technician Technician { get; set; }
}
