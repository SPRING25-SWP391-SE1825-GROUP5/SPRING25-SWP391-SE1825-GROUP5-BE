using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class StaffResponse
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public required string UserFullName { get; set; }
        public required string UserEmail { get; set; }
        public required string UserPhoneNumber { get; set; }
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
