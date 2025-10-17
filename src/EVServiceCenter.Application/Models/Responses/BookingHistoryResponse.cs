using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingHistoryResponse
    {
        public int BookingId { get; set; }
        public string? BookingCode { get; set; }
        public DateOnly BookingDate { get; set; }
        public required string Status { get; set; } = null!;
        
        public CenterInfo CenterInfo { get; set; } = null!;
        public VehicleInfo VehicleInfo { get; set; } = null!;
        public BookingServiceInfo ServiceInfo { get; set; } = null!;
        public TechnicianInfo? TechnicianInfo { get; set; }
        public TimeSlotInfo TimeSlotInfo { get; set; } = null!;
        public CostInfo CostInfo { get; set; } = null!;
        public required List<PartUsedInfo> PartsUsed { get; set; } = new List<PartUsedInfo>();
        public WorkOrderInfo? WorkOrderInfo { get; set; }
        public PaymentInfo? PaymentInfo { get; set; }
        public required List<StatusTimelineInfo> Timeline { get; set; } = new List<StatusTimelineInfo>();
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CenterInfo
    {
        public int CenterId { get; set; }
        public required string CenterName { get; set; } = null!;
        public required string CenterAddress { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }

    public class VehicleInfo
    {
        public int VehicleId { get; set; }
        public required string LicensePlate { get; set; } = null!;
        public required string Vin { get; set; } = null!;
        public string? ModelName { get; set; }
        public string? Brand { get; set; }
        public int? Year { get; set; }
    }

    public class BookingServiceInfo
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; } = null!;
        public required string Description { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int? EstimatedDuration { get; set; }
    }

    public class TechnicianInfo
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Position { get; set; }
        public decimal? Rating { get; set; }
    }

    public class TimeSlotInfo
    {
        public int SlotId { get; set; }
        public required string StartTime { get; set; } = null!;
        public required string EndTime { get; set; } = null!;
    }

    public class CostInfo
    {
        public decimal ServiceCost { get; set; }
        public decimal PartsCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalCost { get; set; }
    }

    public class PartUsedInfo
    {
        public int PartId { get; set; }
        public required string PartName { get; set; } = null!;
        public string? PartNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class WorkOrderInfo
    {
        public int WorkOrderId { get; set; }
        public required string WorkOrderNumber { get; set; } = null!;
        public int? ActualDuration { get; set; }
        public string? WorkPerformed { get; set; }
        public string? CustomerComplaints { get; set; }
        public int? InitialMileage { get; set; }
        public int? FinalMileage { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class PaymentInfo
    {
        public int PaymentId { get; set; }
        public required string PaymentStatus { get; set; } = null!;
        public required string PaymentMethod { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public decimal Amount { get; set; }
    }

    public class StatusTimelineInfo
    {
        public required string Status { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string? Note { get; set; }
    }
}
