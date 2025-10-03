using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateAllTechniciansWeeklyTimeSlotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalTechnicians { get; set; }
        public int TotalTimeSlotsCreated { get; set; }
        public List<TechnicianWeeklyTimeSlotSummary> TechnicianTimeSlots { get; set; } = new List<TechnicianWeeklyTimeSlotSummary>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class TechnicianWeeklyTimeSlotSummary
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public int TimeSlotsCreated { get; set; }
        public List<string> DayNames { get; set; } = new List<string>();
        public List<TechnicianTimeSlotResponse> TimeSlots { get; set; } = new List<TechnicianTimeSlotResponse>();
    }
}