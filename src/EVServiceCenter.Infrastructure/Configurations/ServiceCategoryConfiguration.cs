using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        // Table name
        builder.ToTable("ServiceCategories");

        // Primary key
        builder.HasKey(sc => sc.CategoryId);

        // Properties
        builder.Property(sc => sc.CategoryId)
            .HasColumnName("CategoryID")
            .ValueGeneratedOnAdd();

        builder.Property(sc => sc.CategoryName)
            .HasColumnName("CategoryName")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sc => sc.Description)
            .HasColumnName("Description")
            .HasMaxLength(500);

        builder.Property(sc => sc.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sc => sc.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired()
            .HasDefaultValueSql("getdate()");

        // Indexes
        builder.HasIndex(sc => sc.CategoryName)
            .IsUnique()
            .HasDatabaseName("IX_ServiceCategories_CategoryName");

        // Navigation properties
        builder.HasMany(sc => sc.Services)
            .WithOne(s => s.Category)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
