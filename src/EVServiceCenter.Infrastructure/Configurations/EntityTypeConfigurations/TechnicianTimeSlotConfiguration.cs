using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class TechnicianTimeSlotConfiguration : IEntityTypeConfiguration<TechnicianTimeSlot>
{
    public void Configure(EntityTypeBuilder<TechnicianTimeSlot> entity)
    {
        entity.HasKey(e => e.TechnicianSlotId);
        entity.ToTable("TechnicianTimeSlots", "dbo");

        entity.HasIndex(e => new { e.WorkDate, e.SlotId });
        entity.HasIndex(e => new { e.TechnicianId, e.WorkDate, e.SlotId }).IsUnique();

        entity.Property(e => e.TechnicianSlotId).HasColumnName("TechnicianSlotID");
        entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
        entity.Property(e => e.SlotId).HasColumnName("SlotID");
        entity.Property(e => e.BookingId).HasColumnName("BookingID");

        entity.Property(e => e.Notes).HasMaxLength(255);
        entity.Property(e => e.IsAvailable).HasDefaultValue(true);

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Technician)
            .WithMany(p => p.TechnicianTimeSlots)
            .HasForeignKey(d => d.TechnicianId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Slot)
            .WithMany(p => p.TechnicianTimeSlots)
            .HasForeignKey(d => d.SlotId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


