using System;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateTechnicianTimeSlotRequest
    {
        public int TechnicianId { get; set; }
        public int SlotId { get; set; }
        public DateTime WorkDate { get; set; }
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
    }
}