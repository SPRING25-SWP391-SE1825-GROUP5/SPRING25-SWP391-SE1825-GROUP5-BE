using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianResponse
    {
        public int TechnicianId { get; set; }
        public int UserId { get; set; }
        public required string UserFullName { get; set; }
        public required string UserEmail { get; set; }
        public required string UserPhoneNumber { get; set; }
        public int CenterId { get; set; }
        public required string CenterName { get; set; }
        public required string TechnicianCode { get; set; }
        public required string Specialization { get; set; }
        public int ExperienceYears { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}