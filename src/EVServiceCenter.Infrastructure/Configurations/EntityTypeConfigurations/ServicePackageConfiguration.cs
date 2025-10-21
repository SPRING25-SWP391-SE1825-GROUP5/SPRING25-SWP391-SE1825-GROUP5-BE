using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> entity)
    {
        entity.ToTable("ServicePackages", t =>
        {
            t.HasCheckConstraint("CK_ServicePackages_TotalCredits", "TotalCredits > 0");
            t.HasCheckConstraint("CK_ServicePackages_Price", "Price >= 0");
            t.HasCheckConstraint("CK_ServicePackages_DiscountPercent", "DiscountPercent >= 0 AND DiscountPercent <= 100");
            t.HasCheckConstraint("CK_ServicePackages_ValidDates", "ValidFrom IS NULL OR ValidTo IS NULL OR ValidFrom <= ValidTo");
        });

        entity.HasKey(e => e.PackageId);

        entity.Property(e => e.PackageName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.PackageCode)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.Description)
            .HasMaxLength(500);

        entity.Property(e => e.Price)
            .HasColumnType("decimal(12,2)");

        entity.Property(e => e.DiscountPercent)
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.CreatedAt)
            .HasColumnType("datetime");

        entity.Property(e => e.UpdatedAt)
            .HasColumnType("datetime");

        entity.Property(e => e.ValidFrom)
            .HasColumnType("datetime");

        entity.Property(e => e.ValidTo)
            .HasColumnType("datetime");

        // Foreign Key
        entity.HasOne(e => e.Service)
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint
        entity.HasIndex(e => e.PackageCode)
            .IsUnique();
    }
}
