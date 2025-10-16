using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.HasKey(e => e.NotificationId);
        entity.ToTable("Notifications", "dbo");

        entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.Message).IsRequired();

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.ReadAt).HasPrecision(0);

        entity.HasOne(d => d.User)
            .WithMany(p => p.Notifications)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
