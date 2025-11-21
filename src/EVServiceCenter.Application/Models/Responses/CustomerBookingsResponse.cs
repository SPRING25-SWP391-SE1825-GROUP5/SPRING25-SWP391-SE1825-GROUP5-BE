using System;
using System.Collections.Generic;
using EVServiceCenter.Application.Models;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CustomerBookingsResponse
    {
        public int CustomerId { get; set; }
        public List<CustomerBookingItem> Bookings { get; set; } = new();
        public PaginationInfo? Pagination { get; set; }
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
        
        // Thêm thông tin chi tiết về giá
        public BookingCostBreakdown? CostBreakdown { get; set; }
        public List<BookingPartItem>? PartsUsed { get; set; }
        public List<BookingPromotionItem>? PromotionsApplied { get; set; }
    }

    public class BookingCostBreakdown
    {
        public decimal ServicePrice { get; set; }
        public decimal PartsAmount { get; set; }
        public decimal PackageDiscountAmount { get; set; }
        public decimal PromotionDiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class BookingPartItem
    {
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int QuantityUsed { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BookingPromotionItem
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

