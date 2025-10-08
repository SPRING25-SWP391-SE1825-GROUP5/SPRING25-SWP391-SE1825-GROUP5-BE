using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianSkillResponse
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string SkillDescription { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
