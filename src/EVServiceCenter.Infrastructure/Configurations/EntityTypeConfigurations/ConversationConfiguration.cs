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

        entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
        entity.Property(e => e.Subject).HasMaxLength(255);
        entity.Property(e => e.LastMessageAt).HasColumnName("LastMessageAt");
        entity.Property(e => e.LastMessageId).HasColumnName("LastMessageID");
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(sysutcdatetime())");
        entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
    }
}
