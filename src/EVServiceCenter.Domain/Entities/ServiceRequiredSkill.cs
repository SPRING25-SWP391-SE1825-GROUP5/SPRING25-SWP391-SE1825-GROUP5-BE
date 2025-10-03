using System;

namespace EVServiceCenter.Domain.Entities;

public class ServiceRequiredSkill
{
    public int ServiceId { get; set; }
    public int SkillId { get; set; }

    public virtual Service Service { get; set; }
    public virtual Skill Skill { get; set; }
}
