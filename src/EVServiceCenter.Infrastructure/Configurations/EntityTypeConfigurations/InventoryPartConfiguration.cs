using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class InventoryPartConfiguration : IEntityTypeConfiguration<InventoryPart>
{
    public void Configure(EntityTypeBuilder<InventoryPart> entity)
    {
        entity.HasKey(e => e.InventoryPartId);
        entity.ToTable("InventoryParts", "dbo");

        entity.HasIndex(e => new { e.InventoryId, e.PartId }).IsUnique();

        entity.Property(e => e.InventoryPartId).HasColumnName("InventoryPartID");
        entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
        entity.Property(e => e.PartId).HasColumnName("PartID");
        entity.Property(e => e.LastUpdated)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Inventory)
            .WithMany(p => p.InventoryParts)
            .HasForeignKey(d => d.InventoryId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Part)
            .WithMany(p => p.InventoryParts)
            .HasForeignKey(d => d.PartId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


