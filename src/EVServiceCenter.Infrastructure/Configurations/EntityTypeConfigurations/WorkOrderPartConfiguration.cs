using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class WorkOrderPartConfiguration : IEntityTypeConfiguration<WorkOrderPart>
{
    public void Configure(EntityTypeBuilder<WorkOrderPart> entity)
    {
        entity.HasKey(e => new { e.BookingId, e.PartId });
        entity.ToTable("WorkOrderParts", "dbo");

        entity.HasIndex(e => e.VehicleModelPartId);

        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.PartId).HasColumnName("PartID");
        entity.Property(e => e.VehicleModelPartId).HasColumnName("VehicleModelPartID");

        entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 2)");

        entity.HasOne(d => d.Booking)
            .WithMany()
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Part)
            .WithMany(p => p.WorkOrderParts)
            .HasForeignKey(d => d.PartId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.VehicleModelPart)
            .WithMany()
            .HasForeignKey(d => d.VehicleModelPartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
