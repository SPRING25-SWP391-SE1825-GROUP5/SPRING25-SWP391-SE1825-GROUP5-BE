using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public int CustomerId { get; set; }
        public required string CustomerName { get; set; }
        public int VehicleId { get; set; }
        public required string VehicleInfo { get; set; }
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public DateOnly BookingDate { get; set; }
        public int SlotId { get; set; }
        public required string SlotTime { get; set; }
        public DateOnly? CenterScheduleDate { get; set; }
        public byte? CenterScheduleDayOfWeek { get; set; }

        public required string Status { get; set; }
        public required string SpecialRequests { get; set; }
        
        // Fields migrated from WorkOrder
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public int? CurrentMileage { get; set; }
        public string? LicensePlate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Single-slot model: TotalSlots not used
        public required List<BookingServiceResponse> Services { get; set; } = new List<BookingServiceResponse>();
    }

    public class BookingServiceResponse
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
