using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ServiceCenterConfiguration : IEntityTypeConfiguration<ServiceCenter>
{
    public void Configure(EntityTypeBuilder<ServiceCenter> entity)
    {
        entity.HasKey(e => e.CenterId);
        entity.ToTable("ServiceCenters", "dbo");

        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.CenterName)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Address)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.PhoneNumber).HasMaxLength(20);

        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
    }
}


