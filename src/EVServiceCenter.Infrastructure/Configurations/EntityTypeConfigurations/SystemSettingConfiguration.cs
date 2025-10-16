using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> entity)
    {
        entity.HasKey(e => e.SettingKey);
        entity.ToTable("SystemSettings", "dbo");

        entity.Property(e => e.SettingKey).HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(255);
        entity.Property(e => e.SettingValue).IsRequired();

        entity.Property(e => e.UpdatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
    }
}
