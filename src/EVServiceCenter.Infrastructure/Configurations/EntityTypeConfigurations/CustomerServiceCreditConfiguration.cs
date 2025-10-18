using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public class CustomerServiceCreditConfiguration : IEntityTypeConfiguration<CustomerServiceCredit>
{
    public void Configure(EntityTypeBuilder<CustomerServiceCredit> entity)
    {
        entity.ToTable("CustomerServiceCredits", t =>
        {
            t.HasCheckConstraint("CK_CustomerServiceCredits_TotalCredits", "TotalCredits > 0");
            t.HasCheckConstraint("CK_CustomerServiceCredits_UsedCredits", "UsedCredits >= 0 AND UsedCredits <= TotalCredits");
            t.HasCheckConstraint("CK_CustomerServiceCredits_Status", "Status IN ('ACTIVE', 'EXPIRED', 'USED_UP')");
            t.HasCheckConstraint("CK_CustomerServiceCredits_ExpiryDate", "ExpiryDate IS NULL OR ExpiryDate >= PurchaseDate");
        });

        entity.HasKey(e => e.CreditId);

        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("ACTIVE");

        entity.Property(e => e.CreatedAt)
            .HasColumnType("datetime");

        entity.Property(e => e.UpdatedAt)
            .HasColumnType("datetime");

        entity.Property(e => e.PurchaseDate)
            .HasColumnType("datetime");

        entity.Property(e => e.ExpiryDate)
            .HasColumnType("datetime");

        // Foreign Keys
        entity.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ServicePackage)
            .WithMany(e => e.CustomerServiceCredits)
            .HasForeignKey(e => e.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Service)
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        entity.HasIndex(e => e.CustomerId);
        entity.HasIndex(e => e.PackageId);
        entity.HasIndex(e => e.ServiceId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.ExpiryDate);
        entity.HasIndex(e => new { e.CustomerId, e.ServiceId });
    }
}
