using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EVServiceCenter.Domain.Enums;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class WorkOrderPartConfiguration : IEntityTypeConfiguration<WorkOrderPart>
{
    public void Configure(EntityTypeBuilder<WorkOrderPart> entity)
    {
        // New PK per DB: WorkOrderPartID
        entity.HasKey(e => e.WorkOrderPartId);
        entity.ToTable("WorkOrderParts", "dbo");

        entity.HasIndex(e => e.VehicleModelPartId);

        entity.Property(e => e.WorkOrderPartId).HasColumnName("WorkOrderPartID");
        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.PartId).HasColumnName("PartID");
        entity.Property(e => e.VehicleModelPartId).HasColumnName("VehicleModelPartID");

        entity.Property(e => e.Status).HasConversion<int>();
        // No UnitPrice column mapped; pricing resolved from Parts at time of calculation
        entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.UpdatedAt).HasPrecision(0);
        entity.Property(e => e.ApprovedAt).HasPrecision(0);
        entity.Property(e => e.ConsumedAt).HasPrecision(0);


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
