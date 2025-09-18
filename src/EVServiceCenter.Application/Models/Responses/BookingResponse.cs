using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int VehicleId { get; set; }
        public string VehicleInfo { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public DateOnly BookingDate { get; set; }
        public int StartSlotId { get; set; }
        public string StartSlotTime { get; set; }
        public int EndSlotId { get; set; }
        public string EndSlotTime { get; set; }
        public string Status { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public string SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? TotalSlots { get; set; }
        public List<BookingServiceResponse> Services { get; set; } = new List<BookingServiceResponse>();
        public List<BookingTimeSlotResponse> TimeSlots { get; set; } = new List<BookingTimeSlotResponse>();
    }

    public class BookingServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class BookingTimeSlotResponse
    {
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public string SlotLabel { get; set; }
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public int SlotOrder { get; set; }
    }
}
