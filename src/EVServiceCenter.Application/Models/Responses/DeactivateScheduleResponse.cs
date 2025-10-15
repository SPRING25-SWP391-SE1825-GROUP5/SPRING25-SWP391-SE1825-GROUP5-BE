using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class DeactivateScheduleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalSchedulesUpdated { get; set; }
        public List<CenterScheduleResponse> UpdatedSchedules { get; set; } = new List<CenterScheduleResponse>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> UpdatedDays { get; set; } = new List<string>();
    }
}
