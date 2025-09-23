using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class WeeklyTimeSlotResponse
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
        public DateTime CreatedAt { get; set; }
    }

    public class WeeklyTimeSlotSummaryResponse
    {
        public int CenterId { get; set; }
        public string? CenterName { get; set; }
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int TotalSchedulesCreated { get; set; }
        public List<WeeklyTimeSlotResponse> Schedules { get; set; } = new List<WeeklyTimeSlotResponse>();
        public List<string> DaysOfWeekNames { get; set; } = new List<string>();
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
    }
}
