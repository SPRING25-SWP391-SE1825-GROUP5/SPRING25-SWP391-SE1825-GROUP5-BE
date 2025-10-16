using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.HasKey(e => e.CustomerId);
        entity.ToTable("Customers", "dbo");

        entity.HasIndex(e => e.UserId).IsUnique();

        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.UserId).HasColumnName("UserID");
        entity.Property(e => e.IsGuest).HasDefaultValue(true);

        entity.HasOne(d => d.User)
            .WithOne(p => p.Customer)
            .HasForeignKey<Customer>(d => d.UserId);
    }
}


