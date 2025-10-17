using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AvailabilityResponse
    {
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public DateOnly Date { get; set; }
        public required List<TimeSlotAvailability> TimeSlots { get; set; } = new List<TimeSlotAvailability>();
    }

    public class TimeSlotAvailability
    {
        public int SlotId { get; set; }
        public required string SlotTime { get; set; }
        public required string SlotLabel { get; set; }
        public bool IsAvailable { get; set; }
        public required List<TechnicianAvailability> AvailableTechnicians { get; set; } = new List<TechnicianAvailability>();
    }

    public class TechnicianAvailability
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; }
        public bool IsAvailable { get; set; }
        public required string Status { get; set; }
    }
}
