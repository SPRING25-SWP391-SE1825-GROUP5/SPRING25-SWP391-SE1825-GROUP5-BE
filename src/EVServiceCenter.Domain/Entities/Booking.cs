using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int CenterId { get; set; }



    // Link to TechnicianTimeSlot instead of just SlotId
    public int? TechnicianSlotId { get; set; }

    public string? Status { get; set; }


    public string? SpecialRequests { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }



    // One booking = one service (denormalized)
    public int ServiceId { get; set; }

    // Fields migrated from WorkOrder
    public int? CurrentMileage { get; set; }
    public string? LicensePlate { get; set; }

    // Reserved package credit applied to this booking (nullable)
    public int? AppliedCreditId { get; set; }

    // PayOS orderCode - unique random number để tránh conflict với Order
    public int? PayOSOrderCode { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual TechnicianTimeSlot? TechnicianTimeSlot { get; set; }

    public virtual Vehicle Vehicle { get; set; }

    public virtual Service Service { get; set; }

    public virtual CustomerServiceCredit? AppliedCredit { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
