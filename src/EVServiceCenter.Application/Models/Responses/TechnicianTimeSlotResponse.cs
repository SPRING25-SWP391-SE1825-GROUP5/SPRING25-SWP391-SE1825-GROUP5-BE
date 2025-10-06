using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianTimeSlotResponse
    {
        public int TechnicianSlotId { get; set; }
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public int SlotId { get; set; }
        public string SlotTime { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}