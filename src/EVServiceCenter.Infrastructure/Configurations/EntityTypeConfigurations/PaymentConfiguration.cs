using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.HasKey(e => e.PaymentId);
        entity.ToTable("Payments", "dbo", tb => tb.HasTrigger("tr_Payments_DefaultBuyerFromInvoice"));

        entity.HasIndex(e => e.CreatedAt);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.PaymentCode).IsUnique();

        entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
        entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
        entity.Property(e => e.PaidByUserID).HasColumnName("PaidByUserID");

        entity.Property(e => e.PaymentCode)
            .IsRequired()
            .HasMaxLength(50);
        entity.Property(e => e.PaymentMethod)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("PAYOS");
        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("PENDING");

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.PaidAt).HasPrecision(0);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.PaidByUserID)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Invoice)
            .WithMany(p => p.Payments)
            .HasForeignKey(d => d.InvoiceId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


