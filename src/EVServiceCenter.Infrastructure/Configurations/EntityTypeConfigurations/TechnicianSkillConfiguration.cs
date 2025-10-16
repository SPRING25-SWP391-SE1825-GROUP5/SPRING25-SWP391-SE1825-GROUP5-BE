using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class TechnicianSkillConfiguration : IEntityTypeConfiguration<TechnicianSkill>
{
    public void Configure(EntityTypeBuilder<TechnicianSkill> entity)
    {
        entity.HasKey(e => e.TechnicianSkillId);
        entity.ToTable("TechnicianSkills", "dbo");

        entity.HasIndex(e => new { e.TechnicianId, e.SkillId }).IsUnique();

        entity.Property(e => e.TechnicianSkillId).HasColumnName("TechnicianSkillID");
        entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
        entity.Property(e => e.SkillId).HasColumnName("SkillID");

        entity.HasOne(e => e.Technician)
            .WithMany(t => t.TechnicianSkills)
            .HasForeignKey(e => e.TechnicianId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Skill)
            .WithMany(s => s.TechnicianSkills)
            .HasForeignKey(e => e.SkillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


