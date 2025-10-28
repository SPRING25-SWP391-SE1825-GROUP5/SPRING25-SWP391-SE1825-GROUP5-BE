using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models
{
    public class TechnicianAvailabilityResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<TechnicianAvailabilityData>? Data { get; set; }
        public PaginationInfo? Pagination { get; set; }
        public AvailabilitySummary? Summary { get; set; }
    }

    public class TechnicianAvailabilityData
    {
        public string Date { get; set; } = string.Empty;
        public bool IsFullyBooked { get; set; }
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public List<TechnicianInfo>? Technicians { get; set; }
        
        // Cho API 1 technician
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public List<TimeSlotInfo>? TimeSlots { get; set; }
    }

    public class TechnicianInfo
    {
        public int TechnicianId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int BookedSlots { get; set; }
        public int TotalSlots { get; set; }
    }

    public class TimeSlotInfo
    {
        public string Time { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int? BookingId { get; set; }
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
    }

    public class AvailabilitySummary
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public int TotalTechnicians { get; set; }
        public DateRangeInfo? DateRange { get; set; }
        public StatisticsInfo? Statistics { get; set; }
    }

    public class DateRangeInfo
    {
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }

    public class StatisticsInfo
    {
        public int FullyBookedDays { get; set; }
        public int PartiallyBookedDays { get; set; }
        public int AvailableDays { get; set; }
    }
}
