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
        entity.Property(e => e.PartID)
            .HasColumnName("PartID")
            .IsRequired();
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

        entity.HasIndex(e => new { e.TemplateID, e.PartID }).IsUnique();

        entity.HasOne<ServiceChecklistTemplate>()
            .WithMany()
            .HasForeignKey(e => e.TemplateID);

        // Cấu hình relationship với Part - đảm bảo chỉ dùng PartID, không tạo shadow property PartId
        entity.HasOne(e => e.Part)
            .WithMany()
            .HasForeignKey(e => e.PartID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


