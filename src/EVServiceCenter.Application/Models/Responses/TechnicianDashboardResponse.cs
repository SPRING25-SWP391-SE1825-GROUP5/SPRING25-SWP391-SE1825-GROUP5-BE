using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    // Response cho dashboard overview
    public class TechnicianDashboardResponse
    {
        public TechnicianStats Stats { get; set; } = new TechnicianStats();
        public List<UpcomingBooking> UpcomingBookings { get; set; } = new List<UpcomingBooking>();
        public List<TodayScheduleItem> TodaySchedule { get; set; } = new List<TodayScheduleItem>();
        public PerformanceSummary Performance { get; set; } = new PerformanceSummary();
    }

    public class TechnicianStats
    {
        public int BookingsToday { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedToday { get; set; }
        public double AverageRating { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int ActiveHours { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int InProgressBookings { get; set; }
    }

    public class UpcomingBooking
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
    }

    public class TodayScheduleItem
    {
        public string TimeSlot { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // BOOKED, AVAILABLE, BREAK
        public string? CustomerName { get; set; }
        public int? BookingId { get; set; }
    }

    public class PerformanceSummary
    {
        public PeriodPerformance ThisWeek { get; set; } = new PeriodPerformance();
        public PeriodPerformance ThisMonth { get; set; } = new PeriodPerformance();
    }

    public class PeriodPerformance
    {
        public int BookingsCompleted { get; set; }
        public double AverageRating { get; set; }
        public decimal RevenueGenerated { get; set; }
        public int TotalHoursWorked { get; set; }
    }

    // Response cho booking list
    public class TechnicianBookingListResponse
    {
        public List<TechnicianBookingItem> Bookings { get; set; } = new List<TechnicianBookingItem>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
