using System;
using System.Collections.Generic;
using EVServiceCenter.Domain.Enums;

namespace EVServiceCenter.Domain.Entities;

public partial class MaintenanceReminder
{
    public int ReminderId { get; set; }

    public int VehicleId { get; set; }

    public int? ServiceId { get; set; }

    public int? DueMileage { get; set; }

    public DateOnly? DueDate { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public ReminderType Type { get; set; }

    public ReminderStatus Status { get; set; }

    public int? CadenceDays { get; set; }

    public DateTime? LastSentAt { get; set; }

    public int? PackageId { get; set; }

    public int? UsageIndex { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Vehicle Vehicle { get; set; }

    public virtual Service? Service { get; set; }
}
