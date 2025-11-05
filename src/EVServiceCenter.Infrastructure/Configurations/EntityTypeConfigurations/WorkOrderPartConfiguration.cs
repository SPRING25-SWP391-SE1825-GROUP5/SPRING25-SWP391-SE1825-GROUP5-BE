using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        entity.Property(e => e.CategoryId).HasColumnName("CategoryID");

        entity.Property(e => e.Status).HasColumnType("nvarchar(50)").HasMaxLength(50);
        // No UnitPrice column mapped; pricing resolved from Parts at time of calculation
        // Removed CreatedAt/UpdatedAt/ApprovedAt mappings per requirements
        entity.Property(e => e.ConsumedAt).HasPrecision(0);

        // Map ApprovedByStaffId to column ApprovedByStaffId (new)
        entity.Property<int?>(nameof(WorkOrderPart.ApprovedByStaffId)).HasColumnName("ApprovedByStaffId");


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

        entity.HasOne(d => d.Category)
            .WithMany()
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
