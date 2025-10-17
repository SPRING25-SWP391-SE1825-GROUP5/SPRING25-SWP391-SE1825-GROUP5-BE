using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class MaintenanceChecklistConfiguration : IEntityTypeConfiguration<MaintenanceChecklist>
{
    public void Configure(EntityTypeBuilder<MaintenanceChecklist> entity)
    {
        entity.HasKey(e => e.ChecklistId);
        entity.ToTable("MaintenanceChecklists", "dbo");

        entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
        entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
        entity.Property(e => e.TemplateId).HasColumnName("TemplateId");
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.WorkOrder)
            .WithMany(p => p.MaintenanceChecklists)
            .HasForeignKey(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // TemplateId là FK tới ServiceChecklistTemplate; ràng buộc ở DB
    }
}


