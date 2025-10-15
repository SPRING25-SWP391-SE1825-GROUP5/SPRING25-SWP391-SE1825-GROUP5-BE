using System;

namespace EVServiceCenter.Domain.Entities;

public class MaintenancePolicy
{
    public int PolicyId { get; set; }

    public int IntervalMonths { get; set; }

    public int IntervalKm { get; set; }

    // Removed: MajorEveryMonths, MajorEveryKm

    public bool IsActive { get; set; }

    public int? ServiceId { get; set; }

    public virtual Service? Service { get; set; }
}


