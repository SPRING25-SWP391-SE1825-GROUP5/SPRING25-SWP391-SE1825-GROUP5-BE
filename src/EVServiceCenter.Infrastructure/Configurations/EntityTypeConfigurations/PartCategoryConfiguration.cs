using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public class PartCategoryConfiguration : IEntityTypeConfiguration<PartCategory>
{
    public void Configure(EntityTypeBuilder<PartCategory> builder)
    {
        builder.ToTable("PartCategories");

        builder.HasKey(pc => pc.CategoryId);

        builder.Property(pc => pc.CategoryName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pc => pc.Description)
            .HasMaxLength(500);

        builder.Property(pc => pc.IsActive)
            .HasDefaultValue(true);

        builder.Property(pc => pc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Self-referencing relationship for hierarchical categories
        builder.HasOne(pc => pc.Parent)
            .WithMany(pc => pc.Children)
            .HasForeignKey(pc => pc.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship with PartCategoryMap
        builder.HasMany(pc => pc.PartCategoryMaps)
            .WithOne(pcm => pcm.Category)
            .HasForeignKey(pcm => pcm.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(pc => pc.CategoryName);
        builder.HasIndex(pc => pc.ParentId);
        builder.HasIndex(pc => pc.IsActive);
    }
}
