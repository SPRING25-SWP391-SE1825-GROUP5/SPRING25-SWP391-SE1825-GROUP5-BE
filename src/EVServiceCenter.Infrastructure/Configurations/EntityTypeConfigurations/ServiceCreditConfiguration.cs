using System;
using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ServiceCreditConfiguration : IEntityTypeConfiguration<ServiceCredit>
{
    public void Configure(EntityTypeBuilder<ServiceCredit> entity)
    {
        entity.HasKey(e => e.CreditId);
        entity.ToTable("ServiceCredits", "dbo");

        entity.Property(e => e.CreditId).HasColumnName("CreditID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

        entity.Property(e => e.PriceDiscount).HasColumnType("decimal(10, 2)");
        entity.Property(e => e.ValidFrom).HasConversion(
            v => v.ToDateTime(TimeOnly.MinValue),
            v => DateOnly.FromDateTime(v));
        entity.Property(e => e.ValidTo).HasConversion(
            v => v.ToDateTime(TimeOnly.MinValue),
            v => DateOnly.FromDateTime(v));

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.ServiceCredits)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.Service)
            .WithMany(p => p.ServiceCredits)
            .HasForeignKey(d => d.ServiceId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
