using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class TechnicianTimeSlot
{
    public int TechnicianSlotId { get; set; }

    public int TechnicianId { get; set; }

    public int SlotId { get; set; }

    public DateTime WorkDate { get; set; }

    public bool IsAvailable { get; set; }

    public int? BookingId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual TimeSlot Slot { get; set; }

    public virtual Technician Technician { get; set; }
}
