using System;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateAllTechniciansTimeSlotRequest
    {
        public int CenterId { get; set; }
        public int SlotId { get; set; }
        public DateTime WorkDate { get; set; }
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
    }
}