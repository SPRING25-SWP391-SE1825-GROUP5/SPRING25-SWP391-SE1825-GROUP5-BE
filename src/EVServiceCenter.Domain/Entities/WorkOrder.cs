using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrder
{
    public int WorkOrderId { get; set; }

    public int BookingId { get; set; }

    public int? TechnicianId { get; set; }

    public int? CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public int? CurrentMileage { get; set; }

    public string? LicensePlate { get; set; }

    public int? CenterId { get; set; }

    public int? ServiceId { get; set; }

    public string Status { get; set; }


    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Vehicle? Vehicle { get; set; }

    public virtual ServiceCenter? Center { get; set; }

    public virtual Service? Service { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MaintenanceChecklist> MaintenanceChecklists { get; set; } = new List<MaintenanceChecklist>();

    public virtual Technician Technician { get; set; }

    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
