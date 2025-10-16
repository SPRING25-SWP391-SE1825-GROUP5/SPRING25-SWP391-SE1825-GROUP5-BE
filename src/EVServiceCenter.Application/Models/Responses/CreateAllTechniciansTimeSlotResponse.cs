using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateAllTechniciansTimeSlotResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; } = string.Empty;
        public int TotalTechnicians { get; set; }
        public int TotalTimeSlotsCreated { get; set; }
        public required List<TechnicianTimeSlotSummary> TechnicianTimeSlots { get; set; } = new List<TechnicianTimeSlotSummary>();
        public required List<string> Errors { get; set; } = new List<string>();
    }

    public class TechnicianTimeSlotSummary
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; } = string.Empty;
        public int TimeSlotsCreated { get; set; }
        public required List<TechnicianTimeSlotResponse> TimeSlots { get; set; } = new List<TechnicianTimeSlotResponse>();
    }
}