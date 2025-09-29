using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses;

public class TechnicianBookingsResponse
{
    public int TechnicianId { get; set; }
    public DateOnly Date { get; set; }
    public List<TechnicianBookingItem> Bookings { get; set; } = new();
}

public class TechnicianBookingItem
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; }
    public string Status { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; }
    public int CenterId { get; set; }
    public string CenterName { get; set; }
    public int SlotId { get; set; }
    public string SlotTime { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public string VehiclePlate { get; set; }
    public int? WorkOrderId { get; set; }
    public string WorkOrderStatus { get; set; }
    public DateTime? WorkStartTime { get; set; }
    public DateTime? WorkEndTime { get; set; }
}


