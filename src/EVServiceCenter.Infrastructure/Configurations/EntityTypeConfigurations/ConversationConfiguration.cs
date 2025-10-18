using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> entity)
    {
        entity.HasKey(e => e.ConversationId);
        entity.ToTable("Conversations", "dbo");

        entity.HasIndex(e => e.LastMessageAt);

        // Explicitly configure all properties to avoid auto-mapping issues
        entity.Property(e => e.ConversationId)
            .HasColumnName("ConversationID")
            .ValueGeneratedOnAdd();
        
        entity.Property(e => e.Subject)
            .HasMaxLength(255)
            .IsRequired(false);
        
        entity.Property(e => e.LastMessageAt)
            .IsRequired(false);
        
        entity.Property(e => e.LastMessageId)
            .HasColumnName("LastMessageID")
            .IsRequired(false);
        
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(sysutcdatetime())");
        
        entity.Property(e => e.UpdatedAt)
            .IsRequired(false);

        // Configure relationships
        entity.HasOne(e => e.LastMessage)
            .WithMany()
            .HasForeignKey(e => e.LastMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(e => e.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.ConversationMembers)
            .WithOne()
            .HasForeignKey(cm => cm.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
