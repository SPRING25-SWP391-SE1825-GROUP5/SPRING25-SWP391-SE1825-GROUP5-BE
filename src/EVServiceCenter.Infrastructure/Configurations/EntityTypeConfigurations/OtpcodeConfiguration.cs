using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class OtpcodeConfiguration : IEntityTypeConfiguration<Otpcode>
{
    public void Configure(EntityTypeBuilder<Otpcode> entity)
    {
        entity.HasKey(e => e.Otpid);
        entity.ToTable("OTPCodes", "dbo");

        entity.Property(e => e.Otpid).HasColumnName("OTPID");
        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.ContactInfo)
            .IsRequired()
            .HasMaxLength(100);
        entity.Property(e => e.Otpcode1)
            .IsRequired()
            .HasMaxLength(6)
            .HasColumnName("OTPCode");
        entity.Property(e => e.Otptype)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("OTPType");

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");
        entity.Property(e => e.ExpiresAt).HasPrecision(0);
        entity.Property(e => e.UsedAt).HasPrecision(0);

        entity.HasOne(d => d.User)
            .WithMany(p => p.Otpcodes)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
