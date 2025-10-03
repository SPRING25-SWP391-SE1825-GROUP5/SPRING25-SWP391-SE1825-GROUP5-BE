using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ServiceResponse
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public required string Description { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
