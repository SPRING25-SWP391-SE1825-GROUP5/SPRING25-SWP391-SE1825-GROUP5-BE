using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CenterResponse
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
