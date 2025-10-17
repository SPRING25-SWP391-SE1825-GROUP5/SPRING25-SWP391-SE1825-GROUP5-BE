using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> entity)
    {
        entity.HasKey(e => e.StaffId);
        entity.ToTable("Staff", "dbo");

        entity.Property(e => e.StaffId).HasColumnName("StaffID");
        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Center)
            .WithMany(p => p.Staff)
            .HasForeignKey(d => d.CenterId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.User)
            .WithMany(p => p.Staff)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


