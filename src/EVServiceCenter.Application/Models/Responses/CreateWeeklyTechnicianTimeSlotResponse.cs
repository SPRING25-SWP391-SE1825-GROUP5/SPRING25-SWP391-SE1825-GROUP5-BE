using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateWeeklyTechnicianTimeSlotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<TechnicianTimeSlotResponse> CreatedTimeSlots { get; set; } = new List<TechnicianTimeSlotResponse>();
        public int TotalCreated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}