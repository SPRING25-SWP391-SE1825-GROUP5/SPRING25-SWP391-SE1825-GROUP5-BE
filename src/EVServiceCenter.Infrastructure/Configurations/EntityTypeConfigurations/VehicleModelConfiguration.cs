using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> entity)
    {
        entity.HasKey(e => e.ModelId);
        entity.ToTable("VehicleModel", "dbo");

        entity.HasIndex(e => new { e.ModelName, e.Brand }).IsUnique();

        entity.Property(e => e.ModelId).HasColumnName("ModelID");
        entity.Property(e => e.ModelName)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Brand)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(getdate())");
    }
}


