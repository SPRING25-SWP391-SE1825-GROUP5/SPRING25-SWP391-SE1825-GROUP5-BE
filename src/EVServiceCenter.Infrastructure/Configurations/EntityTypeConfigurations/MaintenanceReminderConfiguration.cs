using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EVServiceCenter.Domain.Enums;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class MaintenanceReminderConfiguration : IEntityTypeConfiguration<MaintenanceReminder>
{
    public void Configure(EntityTypeBuilder<MaintenanceReminder> entity)
    {
        entity.HasKey(e => e.ReminderId);
        entity.ToTable("MaintenanceReminders", "dbo");

        entity.Property(e => e.ReminderId).HasColumnName("ReminderID");
        entity.Property(e => e.VehicleId).HasColumnName("VehicleID");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

        entity.Property(e => e.Type)
            .HasConversion<int>();

        entity.Property(e => e.Status)
            .HasConversion<int>();

        entity.Property(e => e.CompletedAt).HasPrecision(0);
        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Vehicle)
            .WithMany(p => p.MaintenanceReminders)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Service)
            .WithMany()
            .HasForeignKey(d => d.ServiceId);
    }
}
