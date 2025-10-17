using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> entity)
    {
        entity.HasKey(e => e.BookingId);
        entity.ToTable("Bookings", "dbo");

        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.SlotId).HasColumnName("SlotID");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
        entity.Property(e => e.VehicleId).HasColumnName("VehicleID");

        entity.Property(e => e.SpecialRequests).HasMaxLength(500);
        entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("PENDING");
        entity.Property(e => e.TotalCost).HasColumnType("decimal(12, 2)");

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.UpdatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Center)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.CenterId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Slot)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.SlotId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Vehicle)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


