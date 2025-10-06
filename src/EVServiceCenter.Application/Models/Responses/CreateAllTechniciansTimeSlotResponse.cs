using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateAllTechniciansTimeSlotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalTechnicians { get; set; }
        public int TotalTimeSlotsCreated { get; set; }
        public List<TechnicianTimeSlotSummary> TechnicianTimeSlots { get; set; } = new List<TechnicianTimeSlotSummary>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class TechnicianTimeSlotSummary
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public int TimeSlotsCreated { get; set; }
        public List<TechnicianTimeSlotResponse> TimeSlots { get; set; } = new List<TechnicianTimeSlotResponse>();
    }
}