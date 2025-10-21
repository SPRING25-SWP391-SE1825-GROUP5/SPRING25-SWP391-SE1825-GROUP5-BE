using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class VehicleModelPartConfiguration : IEntityTypeConfiguration<VehicleModelPart>
{
    public void Configure(EntityTypeBuilder<VehicleModelPart> entity)
    {
        entity.HasKey(e => e.Id);
        entity.ToTable("VehicleModelParts", "dbo");

        entity.HasIndex(e => new { e.ModelId, e.PartId }).IsUnique();

        entity.Property(e => e.Id).HasColumnName("ID");
        entity.Property(e => e.ModelId).HasColumnName("ModelID");
        entity.Property(e => e.PartId).HasColumnName("PartID");

        // IsCompatible removed
        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(getdate())");

        entity.HasOne(d => d.VehicleModel)
            .WithMany(p => p.VehicleModelParts)
            .HasForeignKey(d => d.ModelId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(d => d.Part)
            .WithMany(p => p.VehicleModelParts)
            .HasForeignKey(d => d.PartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


