using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CustomerResponse
    {
        public int CustomerId { get; set; }
        public int? UserId { get; set; }
        
        public bool IsGuest { get; set; }
        
        
        // Related data
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNumber { get; set; }
        public int VehicleCount { get; set; }
    }
}
