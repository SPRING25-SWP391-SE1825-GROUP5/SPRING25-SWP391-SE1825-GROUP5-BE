using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Technician
{
    public int TechnicianId { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }

    public string Position { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal? Rating { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual ICollection<TechnicianTimeSlot> TechnicianTimeSlots { get; set; } = new List<TechnicianTimeSlot>();


    public virtual User User { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
