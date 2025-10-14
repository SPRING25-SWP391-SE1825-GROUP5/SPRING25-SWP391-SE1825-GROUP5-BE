namespace EVServiceCenter.Domain.Entities;

public class TechnicianSkill
{
    public int TechnicianSkillId { get; set; }
	public int TechnicianId { get; set; }
	public int SkillId { get; set; }

	public virtual Technician Technician { get; set; }
	public virtual Skill Skill { get; set; }
}
