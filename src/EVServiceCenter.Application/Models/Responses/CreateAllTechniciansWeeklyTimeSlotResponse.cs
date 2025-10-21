using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateAllTechniciansWeeklyTimeSlotResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; } = string.Empty;
        public int TotalTechnicians { get; set; }
        public int TotalTimeSlotsCreated { get; set; }
        public required List<TechnicianWeeklyTimeSlotSummary> TechnicianTimeSlots { get; set; } = new List<TechnicianWeeklyTimeSlotSummary>();
        public required List<string> Errors { get; set; } = new List<string>();
    }

    public class TechnicianWeeklyTimeSlotSummary
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; } = string.Empty;
        public int TimeSlotsCreated { get; set; }
        public required List<string> DayNames { get; set; } = new List<string>();
        public required List<TechnicianTimeSlotResponse> TimeSlots { get; set; } = new List<TechnicianTimeSlotResponse>();
    }
}