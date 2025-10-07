using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class MaintenancePolicyResponse
    {
        public int PolicyId { get; set; }
        public int IntervalMonths { get; set; }
        public int IntervalKm { get; set; }
        public bool IsActive { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

