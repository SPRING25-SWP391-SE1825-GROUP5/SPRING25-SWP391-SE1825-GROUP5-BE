using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateWeeklyTechnicianTimeSlotResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; } = string.Empty;
        public required List<TechnicianTimeSlotResponse> CreatedTimeSlots { get; set; } = new List<TechnicianTimeSlotResponse>();
        public int TotalCreated { get; set; }
        public required List<string> Errors { get; set; } = new List<string>();
    }
}