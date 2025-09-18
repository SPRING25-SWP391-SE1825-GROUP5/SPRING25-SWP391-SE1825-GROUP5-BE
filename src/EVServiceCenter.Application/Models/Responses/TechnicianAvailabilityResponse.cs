using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianAvailabilityResponse
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public string TechnicianCode { get; set; }
        public DateOnly Date { get; set; }
        public List<TechnicianTimeSlotAvailability> TimeSlots { get; set; }
    }

    public class TechnicianTimeSlotAvailability
    {
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public string SlotLabel { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsBooked { get; set; }
        public int? BookingId { get; set; }
        public string Notes { get; set; }
    }
}
