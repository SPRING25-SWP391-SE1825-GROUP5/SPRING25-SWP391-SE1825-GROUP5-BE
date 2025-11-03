using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ServiceChecklistTemplateConfiguration : IEntityTypeConfiguration<ServiceChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ServiceChecklistTemplate> entity)
    {
        entity.HasKey(e => e.TemplateID);
        entity.ToTable("ServiceChecklistTemplates", "dbo");

        entity.Property(e => e.TemplateID).HasColumnName("TemplateID");
        entity.Property(e => e.ServiceID).HasColumnName("ServiceID");
        entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
        
        // Maintenance recommendation fields (only fields that exist in database)
        entity.Property(e => e.MinKm).HasColumnName("MinKm");
        entity.Property(e => e.MaxDate).HasColumnName("MaxDate");
        entity.Property(e => e.MaxOverdueDays).HasColumnName("MaxOverdueDays");

        entity.HasOne<Service>()
            .WithMany()
            .HasForeignKey(e => e.ServiceID);
    }
}


