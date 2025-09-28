using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateTechnicianTimeSlotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TechnicianTimeSlotResponse? CreatedTimeSlot { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}