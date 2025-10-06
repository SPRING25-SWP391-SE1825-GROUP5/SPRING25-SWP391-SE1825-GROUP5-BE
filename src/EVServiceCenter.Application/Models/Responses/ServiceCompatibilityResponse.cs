using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ServiceCompatibilityResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; } // Notes from ServicePart relationship
        public DateTime CreatedAt { get; set; }
    }
}
