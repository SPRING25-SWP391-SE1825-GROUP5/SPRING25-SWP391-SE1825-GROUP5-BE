using System;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateTechnicianFullWeekAllSlotsRequest
    {
        public int TechnicianId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? Notes { get; set; }
    }
}


