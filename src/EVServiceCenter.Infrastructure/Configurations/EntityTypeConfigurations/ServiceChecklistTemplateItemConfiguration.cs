using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ServiceChecklistTemplateItemConfiguration : IEntityTypeConfiguration<ServiceChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<ServiceChecklistTemplateItem> entity)
    {
        entity.HasKey(e => e.ItemID);
        entity.ToTable("ServiceChecklistTemplateItems", "dbo");

        entity.Property(e => e.ItemID).HasColumnName("ItemID");
        entity.Property(e => e.TemplateID).HasColumnName("TemplateID");
        entity.Property(e => e.CategoryId)
            .HasColumnName("CategoryID");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

        entity.HasIndex(e => new { e.TemplateID, e.CategoryId })
            .IsUnique()
            .HasFilter("[CategoryID] IS NOT NULL");

        entity.HasOne<ServiceChecklistTemplate>()
            .WithMany()
            .HasForeignKey(e => e.TemplateID);

        entity.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}


