using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class UserPromotionConfiguration : IEntityTypeConfiguration<UserPromotion>
{
    public void Configure(EntityTypeBuilder<UserPromotion> entity)
    {
        entity.ToTable("UserPromotions", "dbo");

        entity.HasIndex(e => new { e.CustomerId, e.UsedAt }).IsDescending(false, true);
        entity.HasIndex(e => new { e.PromotionId, e.UsedAt }).IsDescending(false, true);
        entity.HasIndex(e => new { e.PromotionId, e.BookingId }).IsUnique();
        entity.HasIndex(e => new { e.PromotionId, e.OrderId }).IsUnique();

        entity.Property(e => e.UserPromotionId).HasColumnName("UserPromotionID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.OrderId).HasColumnName("OrderID");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

        entity.Property(e => e.DiscountAmount).HasColumnType("decimal(12, 2)");
        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("USED");

        entity.Property(e => e.UsedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.UserPromotions)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Promotion)
            .WithMany(p => p.UserPromotions)
            .HasForeignKey(d => d.PromotionId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Booking)
            .WithMany()
            .HasForeignKey(d => d.BookingId);

        entity.HasOne(d => d.Order)
            .WithMany()
            .HasForeignKey(d => d.OrderId);

        entity.HasOne(d => d.Service)
            .WithMany()
            .HasForeignKey(d => d.ServiceId);
    }
}
