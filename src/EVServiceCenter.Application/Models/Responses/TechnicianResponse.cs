using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianResponse
    {
        public int TechnicianId { get; set; }
        public int UserId { get; set; }
        public int CenterId { get; set; }
        public string TechnicianCode { get; set; }
        public string Specialization { get; set; }
        public int ExperienceYears { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Related data
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNumber { get; set; }
        public string CenterName { get; set; }
        public string CenterCity { get; set; }
    }
}
