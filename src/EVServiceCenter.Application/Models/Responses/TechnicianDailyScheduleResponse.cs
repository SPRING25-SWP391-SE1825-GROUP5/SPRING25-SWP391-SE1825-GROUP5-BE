using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianDailyScheduleResponse
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public required string DayOfWeek { get; set; } = string.Empty;
        public required List<TimeSlotStatus> TimeSlots { get; set; } = new List<TimeSlotStatus>();
    }

    public class TimeSlotStatus
    {
        public int SlotId { get; set; }
        public required string SlotTime { get; set; } = string.Empty;
        public string? SlotLabel { get; set; }
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
        public int? TechnicianSlotId { get; set; }
    }
}
