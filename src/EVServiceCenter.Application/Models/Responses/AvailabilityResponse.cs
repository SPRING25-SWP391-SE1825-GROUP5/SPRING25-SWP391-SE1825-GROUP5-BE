using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AvailabilityResponse
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public DateOnly Date { get; set; }
        public List<TimeSlotAvailability> TimeSlots { get; set; } = new List<TimeSlotAvailability>();
    }

    public class TimeSlotAvailability
    {
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public string SlotLabel { get; set; }
        public bool IsAvailable { get; set; }
        public List<TechnicianAvailability> AvailableTechnicians { get; set; } = new List<TechnicianAvailability>();
    }

    public class TechnicianAvailability
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public bool IsAvailable { get; set; }
        public string Status { get; set; }
    }
}
