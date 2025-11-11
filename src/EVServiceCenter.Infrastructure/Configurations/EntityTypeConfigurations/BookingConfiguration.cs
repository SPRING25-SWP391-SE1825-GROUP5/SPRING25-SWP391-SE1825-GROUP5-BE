using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> entity)
    {
        entity.HasKey(e => e.BookingId);
        entity.ToTable("Bookings", "dbo", t =>
        {
            // CHECK constraint for Status - must match BookingStatusConstants
            t.HasCheckConstraint("CK_Bookings_Status",
                "[Status] IN ('PENDING', 'CONFIRMED', 'CHECKED_IN', 'IN_PROGRESS', 'COMPLETED', 'PAID', 'CANCELLED')");
        });

        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.TechnicianSlotId).HasColumnName("TechnicianSlotId");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
        entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
        entity.Property(e => e.AppliedCreditId).HasColumnName("AppliedCreditId");
        entity.Property(e => e.PayOSOrderCode).HasColumnName("PayOSOrderCode");

        // Unique index cho PayOSOrderCode để đảm bảo không trùng
        entity.HasIndex(e => e.PayOSOrderCode).IsUnique().HasFilter("[PayOSOrderCode] IS NOT NULL");

        // Fields migrated from WorkOrder
        // TechnicianId removed - now derived from TechnicianTimeSlot
        entity.Property(e => e.CurrentMileage).HasColumnName("CurrentMileage");
        entity.Property(e => e.LicensePlate).HasColumnName("LicensePlate").HasMaxLength(20);

        entity.Property(e => e.SpecialRequests).HasMaxLength(500);
        entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("PENDING");
        // TotalCost removed

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

        entity.HasOne(d => d.TechnicianTimeSlot)
            .WithMany()
            .HasForeignKey(d => d.TechnicianSlotId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Vehicle)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Service)
            .WithMany() // Service doesn't have Bookings collection
            .HasForeignKey(d => d.ServiceId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Foreign key for Technician removed - now derived from TechnicianTimeSlot

        // Applied credit relationship (nullable)
        entity.HasOne(d => d.AppliedCredit)
            .WithMany()
            .HasForeignKey(d => d.AppliedCreditId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


