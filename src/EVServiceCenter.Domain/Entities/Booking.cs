using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int CenterId { get; set; }

    public DateOnly BookingDate { get; set; }

    public int StartSlotId { get; set; }

    public int EndSlotId { get; set; }

    public string Status { get; set; }

    public decimal? TotalEstimatedCost { get; set; }

    public string SpecialRequests { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? TotalSlots { get; set; }

    public 
        ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public  ICollection<BookingTimeSlot> BookingTimeSlots { get; set; } = new List<BookingTimeSlot>();

    public  ServiceCenter Center { get; set; }

    public  Customer Customer { get; set; }

    public  TimeSlot EndSlot { get; set; }

    public  TimeSlot StartSlot { get; set; }

    public  Vehicle Vehicle { get; set; }

    public  ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
