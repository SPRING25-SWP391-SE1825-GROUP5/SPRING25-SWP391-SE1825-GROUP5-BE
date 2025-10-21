using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.UserId);
        entity.ToTable("Users", "dbo");

        entity.HasIndex(e => e.Email).IsUnique();

        entity.Property(e => e.UserId).HasColumnName("UserID");
        entity.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);
        entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        entity.Property(e => e.Address).HasMaxLength(255);
        entity.Property(e => e.AvatarUrl).HasMaxLength(500);
        entity.Property(e => e.Gender).HasMaxLength(6);
        entity.Property(e => e.Role)
            .IsRequired()
            .HasMaxLength(20);

        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.UpdatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
    }
}


