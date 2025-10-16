using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> entity)
    {
        entity.HasKey(e => e.ServiceId);
        entity.ToTable("Services", "dbo");

        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
        entity.Property(e => e.ServiceName)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(500);

        entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
        entity.Property(e => e.IsActive).HasDefaultValue(true);

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
    }
}


