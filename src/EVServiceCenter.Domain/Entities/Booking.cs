using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int CenterId { get; set; }

    public DateOnly BookingDate { get; set; }

    // Single-slot booking: store one SlotId
    public int SlotId { get; set; }

    public string Status { get; set; }

    public decimal? TotalEstimatedCost { get; set; }

    public string? SpecialRequests { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? CenterScheduleId { get; set; }

    public int? TechnicianId { get; set; }

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual ServiceCenter Center { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual TimeSlot Slot { get; set; }

    public virtual Vehicle Vehicle { get; set; }

    public virtual CenterSchedule CenterSchedule { get; set; }

    public virtual Technician Technician { get; set; }

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<ServiceCreditUsage> ServiceCreditUsages { get; set; } = new List<ServiceCreditUsage>();
}
