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
        entity.Property(e => e.BookingId).HasColumnName("BookingID");
        entity.Property(e => e.TemplateId).HasColumnName("TemplateId");
        entity.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValue("PENDING");
        entity.Property(e => e.Notes).HasMaxLength(500);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Booking)
            .WithMany()
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Ràng buộc CHECK cho Status - sẽ được tạo trong migration
        // entity.HasCheckConstraint("CK_MaintenanceChecklists_Status", 
        //     "[Status] IN ('PENDING', 'IN_PROGRESS', 'COMPLETED')");

        // TemplateId là FK tới ServiceChecklistTemplate; ràng buộc ở DB
    }
}


