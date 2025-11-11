using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CustomerBookingsResponse
    {
        public int CustomerId { get; set; }
        public List<CustomerBookingItem> Bookings { get; set; } = new();
    }

    public class CustomerBookingItem
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string SlotTime { get; set; } = string.Empty;
        public string? SlotLabel { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string SpecialRequests { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal? ActualCost { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string? BookingCode { get; set; }
        public string? TechnicianName { get; set; }
    }
}

