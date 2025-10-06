using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public class Skill
{
	public int SkillId { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }

	public virtual ICollection<TechnicianSkill> TechnicianSkills { get; set; } = new List<TechnicianSkill>();
}
