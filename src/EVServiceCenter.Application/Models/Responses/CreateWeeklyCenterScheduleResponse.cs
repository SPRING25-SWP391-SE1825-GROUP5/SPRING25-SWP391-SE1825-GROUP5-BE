using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateWeeklyCenterScheduleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CenterScheduleResponse> CreatedSchedules { get; set; } = new List<CenterScheduleResponse>();
        public int TotalCreated { get; set; }
    }
}
