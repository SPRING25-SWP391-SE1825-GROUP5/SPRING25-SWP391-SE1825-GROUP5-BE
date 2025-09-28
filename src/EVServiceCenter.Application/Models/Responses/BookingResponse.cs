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
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public string Status { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public string SpecialRequests { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Single-slot model: TotalSlots not used
        public List<BookingServiceResponse> Services { get; set; } = new List<BookingServiceResponse>();
    }

    public class BookingServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
