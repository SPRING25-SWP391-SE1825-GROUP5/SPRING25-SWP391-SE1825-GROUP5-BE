using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ServiceRequiredSkillConfiguration : IEntityTypeConfiguration<ServiceRequiredSkill>
{
    public void Configure(EntityTypeBuilder<ServiceRequiredSkill> entity)
    {
        entity.HasKey(e => e.ServiceRequiredSkillId);
        entity.ToTable("ServiceRequiredSkills", "dbo");

        entity.HasIndex(e => new { e.ServiceId, e.SkillId }).IsUnique();

        entity.Property(e => e.ServiceRequiredSkillId).HasColumnName("ServiceRequiredSkillID");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
        entity.Property(e => e.SkillId).HasColumnName("SkillID");

        entity.HasOne(e => e.Service)
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Skill)
            .WithMany()
            .HasForeignKey(e => e.SkillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
