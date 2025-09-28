using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrder
{
    public int WorkOrderId { get; set; }

    public string WorkOrderNumber { get; set; }

    public int BookingId { get; set; }

    public int TechnicianId { get; set; }

    public string Status { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? ActualDuration { get; set; }

    public int? InitialMileage { get; set; }

    public int? FinalMileage { get; set; }

    public string CustomerComplaints { get; set; }

    public string WorkPerformed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MaintenanceChecklist> MaintenanceChecklists { get; set; } = new List<MaintenanceChecklist>();

    public virtual Technician Technician { get; set; }

    public virtual ICollection<WorkOrderChargeProposal> WorkOrderChargeProposals { get; set; } = new List<WorkOrderChargeProposal>();

    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();

    public virtual ICollection<ServiceCreditUsage> ServiceCreditUsages { get; set; } = new List<ServiceCreditUsage>();
}
