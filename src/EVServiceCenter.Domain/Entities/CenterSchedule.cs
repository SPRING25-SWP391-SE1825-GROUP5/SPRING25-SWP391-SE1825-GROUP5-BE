using System;

namespace EVServiceCenter.Domain.Entities;

public partial class CenterSchedule
{
    public int CenterScheduleId { get; set; }

    public int CenterId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly? ScheduleDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ServiceCenter Center { get; set; }
}