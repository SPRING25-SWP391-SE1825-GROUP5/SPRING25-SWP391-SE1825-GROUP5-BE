using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateTechnicianFullWeekAllSlotsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public int TotalSlotsCreated { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}


