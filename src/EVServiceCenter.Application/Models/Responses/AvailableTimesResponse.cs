using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AvailableTimesResponse
    {
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public DateOnly Date { get; set; }
        public int? TechnicianId { get; set; }
        public required string TechnicianName { get; set; }
        public required List<AvailableTimeSlot> AvailableTimeSlots { get; set; } = new List<AvailableTimeSlot>();
        public required List<ServiceInfo> AvailableServices { get; set; } = new List<ServiceInfo>();
    }

    public class AvailableTimeSlot
    {
        public int SlotId { get; set; }
        public TimeOnly SlotTime { get; set; }
        public required string SlotLabel { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsRealtimeAvailable { get; set; }
        public int? TechnicianId { get; set; }
        public required string TechnicianName { get; set; }
        public required string Status { get; set; } // AVAILABLE, BOOKED, MAINTENANCE, BREAK
        public DateTime LastUpdated { get; set; }
    }

    public class ServiceInfo
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public required string Description { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
    }
}


















