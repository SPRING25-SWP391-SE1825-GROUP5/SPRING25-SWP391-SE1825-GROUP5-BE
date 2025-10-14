using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianDailyScheduleResponse
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public List<TimeSlotStatus> TimeSlots { get; set; } = new List<TimeSlotStatus>();
    }

    public class TimeSlotStatus
    {
        public int SlotId { get; set; }
        public string SlotTime { get; set; } = string.Empty;
        public string SlotLabel { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
        public int? TechnicianSlotId { get; set; }
    }
}
