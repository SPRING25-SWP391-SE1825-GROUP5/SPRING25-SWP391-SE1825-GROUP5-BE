using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> entity)
    {
        entity.HasKey(e => e.TechnicianId);
        entity.ToTable("Technicians", "dbo");

        entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");
        entity.Property(e => e.CenterId).HasColumnName("CenterID");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.Position).HasMaxLength(100);
        entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)").HasDefaultValue(null);
        entity.Property(e => e.IsActive).HasDefaultValue(true);

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Center)
            .WithMany(p => p.Technicians)
            .HasForeignKey(d => d.CenterId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.User)
            .WithMany(p => p.Technicians)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}


