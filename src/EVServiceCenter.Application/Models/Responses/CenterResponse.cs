using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CenterResponse
    {
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public required string Address { get; set; }
        public required string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
