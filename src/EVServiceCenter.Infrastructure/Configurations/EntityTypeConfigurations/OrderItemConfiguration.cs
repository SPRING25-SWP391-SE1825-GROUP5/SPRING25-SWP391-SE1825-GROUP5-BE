using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.HasKey(e => e.OrderItemId);
        entity.ToTable("OrderItems", "dbo");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");
        entity.Property(e => e.OrderId).HasColumnName("OrderID");
        entity.Property(e => e.PartId).HasColumnName("PartID");

        entity.Property(e => e.ConsumedQty).HasColumnName("ConsumedQty");

        entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

        entity.HasOne(d => d.Order)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Part)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(d => d.PartId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


