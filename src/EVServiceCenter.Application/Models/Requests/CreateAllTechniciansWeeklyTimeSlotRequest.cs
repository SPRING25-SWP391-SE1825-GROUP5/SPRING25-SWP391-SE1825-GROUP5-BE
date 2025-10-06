using System;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateAllTechniciansWeeklyTimeSlotRequest
    {
        public int CenterId { get; set; }
        public int SlotId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
    }
}