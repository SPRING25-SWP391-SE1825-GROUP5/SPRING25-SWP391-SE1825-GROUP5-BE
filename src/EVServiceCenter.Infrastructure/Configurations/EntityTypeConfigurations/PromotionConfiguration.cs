using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> entity)
    {
        entity.ToTable("Promotions", "dbo");

        entity.HasIndex(e => new { e.StartDate, e.EndDate });
        entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate });
        entity.HasIndex(e => e.Code).IsUnique();

        entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
        entity.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(30);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.DiscountType)
            .IsRequired()
            .HasMaxLength(10);
        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20);

        entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
        entity.Property(e => e.MaxDiscount).HasColumnType("decimal(12, 2)");
        entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 2)");

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.UpdatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
    }
}
