using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceReminder
{
    public int ReminderId { get; set; }

    public int VehicleId { get; set; }

    public string ServiceType { get; set; }

    public int? DueMileage { get; set; }

    public DateOnly? DueDate { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Vehicle Vehicle { get; set; }
}
