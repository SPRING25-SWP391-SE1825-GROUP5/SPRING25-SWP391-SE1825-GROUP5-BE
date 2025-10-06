using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateWorkOrderRequest
    {
        public int? BookingId { get; set; }

        // Walk-in: nếu không có BookingId, các trường sau bắt buộc
        public int? CenterId { get; set; }
        public int? ServiceId { get; set; }
        public int? CustomerId { get; set; }
        public int? VehicleId { get; set; }

        public int? TechnicianId { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "NOT_STARTED";

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}


