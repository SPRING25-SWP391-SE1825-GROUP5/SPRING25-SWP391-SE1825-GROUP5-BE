using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    // Deprecated: giữ để tránh lỗi compile nếu còn endpoint cũ; không dùng nữa
    public class AssignBookingServicesRequest { }

    public class BookingServiceRequest { public int ServiceId { get; set; } }
}
