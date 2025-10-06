using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int CenterId { get; set; }

    

    // Single-slot booking: store one SlotId
    public int SlotId { get; set; }

    public string? Status { get; set; }

    public decimal? TotalCost { get; set; }

    public string? SpecialRequests { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    

    // One booking = one service (denormalized)
    public int ServiceId { get; set; }


    public virtual ServiceCenter Center { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual TimeSlot Slot { get; set; }

    public virtual Vehicle Vehicle { get; set; }

    public virtual Service Service { get; set; }

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
