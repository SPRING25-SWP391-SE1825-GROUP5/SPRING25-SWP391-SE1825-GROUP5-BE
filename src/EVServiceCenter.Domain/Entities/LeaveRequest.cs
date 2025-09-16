using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class LeaveRequest
{
    public int RequestId { get; set; }

    public int TechnicianId { get; set; }

    public string LeaveType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int TotalDays { get; set; }

    public string Reason { get; set; }

    public string Status { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string Comments { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User ApprovedByNavigation { get; set; }

    public virtual Technician Technician { get; set; }
}
