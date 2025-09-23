using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CenterScheduleResponse
    {
        public int CenterScheduleId { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public byte DayOfWeek { get; set; }
        public string DayOfWeekName { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        // SlotLength removed; system assumes 30-minute slots
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public int CapacityTotal { get; set; }
        public int CapacityLeft { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
