using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingTodayResponse
    {
        public List<BookingTodayItem> Bookings { get; set; } = new List<BookingTodayItem>();
        public BookingTodaySummary Summary { get; set; } = new BookingTodaySummary();
    }

    public class BookingTodayItem
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SlotTime { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BookingTodaySummary
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class BookingListResponse
    {
        public List<BookingListItem> Bookings { get; set; } = new List<BookingListItem>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class BookingListItem
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string SlotTime { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public decimal PartsPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
