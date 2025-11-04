using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingResponse
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public required string CustomerName { get; set; }
        public int VehicleId { get; set; }
        public required string VehicleInfo { get; set; }
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public DateOnly BookingDate { get; set; }
        public int? TechnicianSlotId { get; set; }
        public int SlotId { get; set; } // Keep for backward compatibility
        public required string SlotTime { get; set; }

        public required string Status { get; set; }
        public required string SpecialRequests { get; set; }

        // Fields migrated from WorkOrder
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public int? CurrentMileage { get; set; }
        public string? LicensePlate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Package information
        public int? AppliedCreditId { get; set; }
        public string? PackageCode { get; set; }
        public string? PackageName { get; set; }
        public decimal? PackageDiscountPercent { get; set; }
        public decimal? PackageDiscountAmount { get; set; }
        public decimal? OriginalServicePrice { get; set; }

        // Payment information
        public decimal TotalAmount { get; set; }
        public string PaymentType { get; set; } = "SERVICE"; // "SERVICE" hoặc "PACKAGE"

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
