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
        entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
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

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.Invoices)
            .HasForeignKey(d => d.CustomerId);

        entity.HasOne(d => d.WorkOrder)
            .WithMany(p => p.Invoices)
            .HasForeignKey(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Order)
            .WithMany(p => p.Invoices)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
