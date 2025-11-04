using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianPerformanceResponse
    {
        public List<TechnicianPerformanceItem> Technicians { get; set; } = new List<TechnicianPerformanceItem>();
        public TechnicianPerformanceSummary Summary { get; set; } = new TechnicianPerformanceSummary();
    }

    public class TechnicianPerformanceItem
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int PendingBookings { get; set; }
        public double AverageRating { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerBooking { get; set; }
        public double AverageProcessingTimeHours { get; set; }
        public DateTime LastActiveDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class TechnicianPerformanceSummary
    {
        public int TotalTechnicians { get; set; }
        public int ActiveTechnicians { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public double AverageProcessingTimeHours { get; set; }
    }

    public class TechnicianScheduleResponse
    {
        public string Date { get; set; } = string.Empty;
        public List<TechnicianScheduleItem> Technicians { get; set; } = new List<TechnicianScheduleItem>();
    }

    public class TechnicianScheduleItem
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public List<ScheduleSlot> Slots { get; set; } = new List<ScheduleSlot>();
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public double UtilizationRate { get; set; }
    }

    public class ScheduleSlot
    {
        public int SlotId { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // AVAILABLE, BOOKED, UNAVAILABLE
        public int? BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response cho API GetTechnicianBookingStats - thống kê số lượng booking của center và mỗi technician
    /// </summary>
    public class TechnicianBookingStatsResponse
    {
        public bool Success { get; set; } = true;
        public int TotalBookings { get; set; }
        public List<TechnicianBookingStatsItem> Technicians { get; set; } = new List<TechnicianBookingStatsItem>();
    }

    /// <summary>
    /// Item trong danh sách thống kê booking của technician
    /// </summary>
    public class TechnicianBookingStatsItem
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
    }

    /// <summary>
    /// Response cho API GetCenterUtilizationRate - tỉ lệ lấp đầy của center
    /// </summary>
    public class UtilizationRateResponse
    {
        public bool Success { get; set; } = true;
        public decimal AverageUtilizationRate { get; set; }
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public string? Granularity { get; set; }
        public List<UtilizationRateByPeriodItem> Items { get; set; } = new List<UtilizationRateByPeriodItem>();
    }

    /// <summary>
    /// Item trong danh sách tỉ lệ lấp đầy theo period
    /// </summary>
    public class UtilizationRateByPeriodItem
    {
        public string Period { get; set; } = string.Empty;
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public decimal UtilizationRate { get; set; }
    }
}
