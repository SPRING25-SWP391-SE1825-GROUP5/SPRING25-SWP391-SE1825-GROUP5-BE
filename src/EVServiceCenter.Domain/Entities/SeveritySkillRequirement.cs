namespace EVServiceCenter.Domain.Entities;

public class SeveritySkillRequirement
{
	public byte Severity { get; set; } // 1..5
	public int SkillId { get; set; }
	public byte MinLevel { get; set; } // 1..5
	public string Notes { get; set; }

	public virtual Skill Skill { get; set; }
}


