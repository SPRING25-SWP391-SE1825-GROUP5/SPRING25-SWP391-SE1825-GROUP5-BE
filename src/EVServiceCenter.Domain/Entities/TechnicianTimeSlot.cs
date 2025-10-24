using System;
using System.Collections.Generic;
using EVServiceCenter.Domain.Enums;

namespace EVServiceCenter.Domain.Entities;

public partial class TechnicianTimeSlot
{
    public int TechnicianSlotId { get; set; }

    public int TechnicianId { get; set; }

    public int SlotId { get; set; }

    public DateTime WorkDate { get; set; }

    public bool IsAvailable { get; set; }

    public int? BookingId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual TimeSlot Slot { get; set; }

    public virtual Technician Technician { get; set; }

    /// <summary>
    /// Xác định trạng thái của slot dựa trên IsAvailable và BookingId
    /// </summary>
    public TechnicianTimeSlotStatus GetStatus()
    {
        if (IsAvailable && BookingId == null)
            return TechnicianTimeSlotStatus.Available;
        else if (!IsAvailable && BookingId == null)
            return TechnicianTimeSlotStatus.Reserved;
        else if (!IsAvailable && BookingId != null)
            return TechnicianTimeSlotStatus.Assigned;
        else
            return TechnicianTimeSlotStatus.Released;
    }

    /// <summary>
    /// Kiểm tra slot có thể được assign cho booking mới không
    /// </summary>
    public bool CanBeAssigned()
    {
        return GetStatus() == TechnicianTimeSlotStatus.Available;
    }

    /// <summary>
    /// Kiểm tra slot có đang được assign cho booking khác không
    /// </summary>
    public bool IsAssignedToOtherBooking(int? currentBookingId)
    {
        return GetStatus() == TechnicianTimeSlotStatus.Assigned && 
               BookingId != null && 
               BookingId != currentBookingId;
    }
}
