using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> entity)
    {
        entity.HasKey(e => e.InvoiceId);
        entity.ToTable("Invoices", "dbo");

        entity.HasIndex(e => e.Status);

        entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        // WorkOrderId removed - functionality merged into Booking
        entity.Property(e => e.OrderId).HasColumnName("OrderID");

        entity.Property(e => e.Email).HasMaxLength(255);
        entity.Property(e => e.Phone).HasMaxLength(20);
        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("DRAFT");

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        // New discount columns
        entity.Property(e => e.PackageDiscountAmount)
            .HasColumnType("decimal(12, 2)")
            .HasDefaultValue(0);
        entity.Property(e => e.PromotionDiscountAmount)
            .HasColumnType("decimal(12, 2)")
            .HasDefaultValue(0);

        // New: PartsAmount snapshot on invoice
        entity.Property(e => e.PartsAmount)
            .HasColumnType("decimal(12, 2)")
            .HasDefaultValue(0);

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.Invoices)
            .HasForeignKey(d => d.CustomerId);

        // WorkOrder relationship removed - functionality merged into Booking

        entity.HasOne(d => d.Order)
            .WithMany(p => p.Invoices)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
