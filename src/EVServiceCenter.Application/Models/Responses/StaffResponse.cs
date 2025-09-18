using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class StaffResponse
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNumber { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public string StaffCode { get; set; }
        public string Position { get; set; }
        public DateOnly HireDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
