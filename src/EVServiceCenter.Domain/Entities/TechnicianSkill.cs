namespace EVServiceCenter.Domain.Entities;

public class TechnicianSkill
{
	public int TechnicianId { get; set; }
	public int SkillId { get; set; }
	public byte Level { get; set; } // 1-5
	public byte Years { get; set; }

	public virtual Technician Technician { get; set; }
	public virtual Skill Skill { get; set; }
}
