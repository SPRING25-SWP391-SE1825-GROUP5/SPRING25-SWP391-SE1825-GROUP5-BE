using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AvailableTimesResponse
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public DateOnly Date { get; set; }
        public int? TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public List<AvailableTimeSlot> AvailableTimeSlots { get; set; } = new List<AvailableTimeSlot>();
        public List<ServiceInfo> AvailableServices { get; set; } = new List<ServiceInfo>();
    }

    public class AvailableTimeSlot
    {
        public int SlotId { get; set; }
        public TimeOnly SlotTime { get; set; }
        public string SlotLabel { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsRealtimeAvailable { get; set; }
        public int? TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public string Status { get; set; } // AVAILABLE, BOOKED, MAINTENANCE, BREAK
        public DateTime LastUpdated { get; set; }
    }

    public class ServiceInfo
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public int EstimatedDuration { get; set; }
        public int RequiredSlots { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
    }
}


















