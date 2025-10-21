using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses;

public class TechnicianBookingsResponse
{
    public int TechnicianId { get; set; }
    public DateOnly Date { get; set; }
    public required List<TechnicianBookingItem> Bookings { get; set; } = new();
}

public class TechnicianBookingItem
{
    public int BookingId { get; set; }
    public required string BookingCode { get; set; }
    public required string Status { get; set; }
    public int ServiceId { get; set; }
    public required string ServiceName { get; set; }
    public int CenterId { get; set; }
    public required string CenterName { get; set; }
    public int SlotId { get; set; }
    public required string SlotTime { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerPhone { get; set; }
    public required string VehiclePlate { get; set; }
    public int? WorkOrderId { get; set; }
    public required string WorkOrderStatus { get; set; }
    public DateTime? WorkStartTime { get; set; }
    public DateTime? WorkEndTime { get; set; }
}


