using System;
using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> entity)
    {
        entity.HasKey(e => e.SlotId);
        entity.ToTable("TimeSlots", "dbo");

        entity.HasIndex(e => e.SlotTime).IsUnique();

        entity.Property(e => e.SlotId).HasColumnName("SlotID");
        entity.Property(e => e.SlotLabel)
            .IsRequired()
            .HasMaxLength(20);
        entity.Property(e => e.SlotTime).HasConversion(
            v => v.ToTimeSpan(),
            v => TimeOnly.FromTimeSpan(v));

        entity.Property(e => e.IsActive).HasDefaultValue(true);
    }
}


