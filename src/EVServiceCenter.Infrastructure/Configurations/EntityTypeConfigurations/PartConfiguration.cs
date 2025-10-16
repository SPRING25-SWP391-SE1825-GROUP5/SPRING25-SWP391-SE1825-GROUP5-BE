using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> entity)
    {
        entity.HasKey(e => e.PartId);
        entity.ToTable("Parts", "dbo");

        entity.HasIndex(e => e.PartNumber).IsUnique();

        entity.Property(e => e.PartId).HasColumnName("PartID");
        entity.Property(e => e.PartName)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.PartNumber)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.Brand).HasMaxLength(50);
        entity.Property(e => e.ImageUrl).HasMaxLength(255);

        entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
        entity.Property(e => e.IsActive).HasDefaultValue(true);

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
    }
}


