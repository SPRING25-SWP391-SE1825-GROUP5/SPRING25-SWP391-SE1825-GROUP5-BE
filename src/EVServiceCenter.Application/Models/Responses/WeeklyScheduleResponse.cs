using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class WeeklyScheduleResponse
    {
        public int WeeklyScheduleId { get; set; }
        public int CenterId { get; set; }
        public string? CenterName { get; set; }
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public byte DayOfWeek { get; set; }
        public string DayOfWeekName { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public TimeOnly? BreakStart { get; set; }
        public TimeOnly? BreakEnd { get; set; }
        public byte BufferMinutes { get; set; }
        public byte StepMinutes { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }
}

