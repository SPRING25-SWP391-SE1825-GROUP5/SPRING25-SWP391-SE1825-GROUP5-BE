using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ServiceResponse
    {
        public int ServiceId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public int EstimatedDuration { get; set; }
        public int RequiredSlots { get; set; }
        public decimal BasePrice { get; set; }
        public string RequiredSkills { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
