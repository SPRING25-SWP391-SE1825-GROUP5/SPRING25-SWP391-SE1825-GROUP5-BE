using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(e => e.OrderId);
        entity.ToTable("Orders", "dbo");

        entity.Property(e => e.OrderId).HasColumnName("OrderID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.PayOSOrderCode).HasColumnName("PayOSOrderCode");

        // Unique index cho PayOSOrderCode để đảm bảo không trùng
        entity.HasIndex(e => e.PayOSOrderCode).IsUnique().HasFilter("[PayOSOrderCode] IS NOT NULL");

        entity.Property(e => e.Status).HasDefaultValue("PENDING");

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.UpdatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.Orders)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


