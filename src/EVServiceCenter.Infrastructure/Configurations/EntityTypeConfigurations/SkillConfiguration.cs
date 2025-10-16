using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> entity)
    {
        entity.HasKey(e => e.SkillId);
        entity.ToTable("Skills", "dbo");

        entity.HasIndex(e => e.Name).IsUnique();

        entity.Property(e => e.SkillId).HasColumnName("SkillID");
        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(255);
    }
}


