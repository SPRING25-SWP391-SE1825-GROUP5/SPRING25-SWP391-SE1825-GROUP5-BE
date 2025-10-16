using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianSkillResponse
    {
        public int TechnicianId { get; set; }
        public required string TechnicianName { get; set; } = string.Empty;
        public int SkillId { get; set; }
        public required string SkillName { get; set; } = string.Empty;
        public required string SkillDescription { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
