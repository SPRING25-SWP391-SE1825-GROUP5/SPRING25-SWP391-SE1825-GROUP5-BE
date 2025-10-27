using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public class PartCategoryMapConfiguration : IEntityTypeConfiguration<PartCategoryMap>
{
    public void Configure(EntityTypeBuilder<PartCategoryMap> builder)
    {
        builder.ToTable("PartCategoryMaps");

        // Composite primary key
        builder.HasKey(pcm => new { pcm.PartId, pcm.CategoryId });

        builder.Property(pcm => pcm.IsPrimary)
            .HasDefaultValue(false);

        builder.Property(pcm => pcm.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Many-to-one relationship with Part
        builder.HasOne(pcm => pcm.Part)
            .WithMany()
            .HasForeignKey(pcm => pcm.PartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one relationship with PartCategory
        builder.HasOne(pcm => pcm.Category)
            .WithMany(pc => pc.PartCategoryMaps)
            .HasForeignKey(pcm => pcm.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(pcm => pcm.PartId);
        builder.HasIndex(pcm => pcm.CategoryId);
        builder.HasIndex(pcm => pcm.IsPrimary);
    }
}
