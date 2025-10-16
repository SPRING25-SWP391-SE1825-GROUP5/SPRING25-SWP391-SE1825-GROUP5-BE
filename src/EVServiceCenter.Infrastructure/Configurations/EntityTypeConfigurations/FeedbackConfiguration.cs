using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> entity)
    {
        entity.HasKey(e => e.FeedbackId);
        entity.ToTable("Feedbacks", "dbo");

        entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
        entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
        entity.Property(e => e.OrderId).HasColumnName("OrderID");
        entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
        entity.Property(e => e.PartId).HasColumnName("PartID");
        entity.Property(e => e.TechnicianId).HasColumnName("TechnicianID");

        entity.Property(e => e.IsAnonymous).HasDefaultValue(false);

        entity.Property(e => e.CreatedAt)
            .HasPrecision(0)
            .HasDefaultValueSql("(sysdatetime())");

        entity.HasOne(d => d.Customer)
            .WithMany(p => p.Feedbacks)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Order)
            .WithMany(p => p.Feedbacks)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.WorkOrder)
            .WithMany(p => p.Feedbacks)
            .HasForeignKey(d => d.WorkOrderId)
            .OnDelete(DeleteBehavior.ClientSetNull);
        
        entity.HasOne(d => d.Part)
            .WithMany()
            .HasForeignKey(d => d.PartId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(d => d.Technician)
            .WithMany()
            .HasForeignKey(d => d.TechnicianId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
