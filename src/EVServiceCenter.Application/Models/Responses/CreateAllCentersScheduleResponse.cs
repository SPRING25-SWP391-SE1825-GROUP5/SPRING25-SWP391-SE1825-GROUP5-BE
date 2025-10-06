using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateAllCentersScheduleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalCenters { get; set; }
        public int TotalSchedulesCreated { get; set; }
        public List<CenterScheduleSummary> CenterSchedules { get; set; } = new List<CenterScheduleSummary>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class CenterScheduleSummary
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public int SchedulesCreated { get; set; }
        public List<string> DayNames { get; set; } = new List<string>();
    }
}
