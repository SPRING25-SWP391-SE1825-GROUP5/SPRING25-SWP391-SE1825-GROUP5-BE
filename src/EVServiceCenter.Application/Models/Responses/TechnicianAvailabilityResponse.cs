using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianAvailabilityResponse
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public required List<TimeSlotAvailability> AvailableSlots { get; set; } = new List<TimeSlotAvailability>();
    }
}