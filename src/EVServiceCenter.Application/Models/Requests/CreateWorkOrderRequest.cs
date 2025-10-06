using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateWorkOrderRequest
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int TechnicianId { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "NOT_STARTED";

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}


