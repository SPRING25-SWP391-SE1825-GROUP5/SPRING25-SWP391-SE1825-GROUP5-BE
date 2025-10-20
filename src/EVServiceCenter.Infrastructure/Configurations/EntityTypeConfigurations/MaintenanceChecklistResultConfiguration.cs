using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class MaintenanceChecklistResultConfiguration : IEntityTypeConfiguration<MaintenanceChecklistResult>
{
    public void Configure(EntityTypeBuilder<MaintenanceChecklistResult> entity)
    {
        entity.HasKey(e => e.ResultId);
        entity.ToTable("MaintenanceChecklistResults", "dbo");

        entity.Property(e => e.ResultId).HasColumnName("ResultID").ValueGeneratedOnAdd();
        entity.Property(e => e.ChecklistId).HasColumnName("ChecklistID");
        entity.Property(e => e.PartId).HasColumnName("PartID");

        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.Result).HasMaxLength(50);
        entity.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValue("PENDING");

        entity.HasOne(d => d.Checklist)
            .WithMany(p => p.MaintenanceChecklistResults)
            .HasForeignKey(d => d.ChecklistId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Part)
            .WithMany()
            .HasForeignKey(d => d.PartId);
    }
}
