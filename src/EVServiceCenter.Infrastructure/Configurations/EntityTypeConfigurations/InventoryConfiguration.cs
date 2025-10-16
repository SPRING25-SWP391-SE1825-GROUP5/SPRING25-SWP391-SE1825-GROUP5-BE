using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> entity)
    {
        entity.HasKey(e => e.InventoryId);
        entity.ToTable("Inventory", "dbo");

        entity.HasIndex(e => e.CenterId).IsUnique();

        entity.Property(e => e.InventoryId).HasColumnName("InventoryID");
        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.LastUpdated)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Center)
            .WithMany(p => p.Inventories)
            .HasForeignKey(d => d.CenterId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


