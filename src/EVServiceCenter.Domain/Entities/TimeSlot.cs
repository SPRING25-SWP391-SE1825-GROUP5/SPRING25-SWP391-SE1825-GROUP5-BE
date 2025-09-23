using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class TimeSlot
{
    public int SlotId { get; set; }

    public TimeOnly SlotTime { get; set; }

    public string? SlotLabel { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<BookingTimeSlot> BookingTimeSlots { get; set; } = new List<BookingTimeSlot>();

    public virtual ICollection<TechnicianTimeSlot> TechnicianTimeSlots { get; set; } = new List<TechnicianTimeSlot>();
}
