using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> entity)
    {
        entity.HasKey(e => e.WorkOrderId);
        entity.ToTable("WorkOrders", "dbo");

        entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("NOT_STARTED");
        entity.Property(e => e.LicensePlate).HasMaxLength(20);

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.UpdatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Booking)
            .WithMany(p => p.WorkOrders)
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Technician)
            .WithMany(p => p.WorkOrders)
            .HasForeignKey(d => d.TechnicianId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Vehicle)
            .WithMany()
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Center)
            .WithMany()
            .HasForeignKey(d => d.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Service)
            .WithMany()
            .HasForeignKey(d => d.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


