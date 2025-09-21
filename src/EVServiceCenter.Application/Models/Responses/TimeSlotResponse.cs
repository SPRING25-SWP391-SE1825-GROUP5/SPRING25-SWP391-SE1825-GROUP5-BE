using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TimeSlotResponse
    {
        public int SlotId { get; set; }
        public TimeOnly SlotTime { get; set; }
        public required string SlotLabel { get; set; }
        public bool IsActive { get; set; }
    }
}
