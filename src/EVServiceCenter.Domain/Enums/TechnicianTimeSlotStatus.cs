namespace EVServiceCenter.Domain.Enums;

public enum TechnicianTimeSlotStatus
{
    Available,      // IsAvailable = true, BookingId = null
    Reserved,       // IsAvailable = false, BookingId = null  
    Assigned,       // IsAvailable = false, BookingId != null
    Released        // IsAvailable = true, BookingId = null (có history)
}
