using System;
using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> entity)
    {
        entity.HasKey(e => e.VehicleId);
        entity.ToTable("Vehicles", "dbo");

        entity.HasIndex(e => e.LicensePlate).IsUnique();
        entity.HasIndex(e => e.Vin).IsUnique();

        entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.ModelId).HasColumnName("ModelID");

        entity.Property(e => e.LicensePlate)
            .IsRequired()
            .HasMaxLength(20);
        entity.Property(e => e.Vin)
            .IsRequired()
            .HasMaxLength(17)
            .HasColumnName("VIN");
        entity.Property(e => e.Color).HasMaxLength(30);

        // PurchaseDate removed

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.Vehicles)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.VehicleModel)
            .WithMany(p => p.Vehicles)
            .HasForeignKey(d => d.ModelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


