using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> entity)
    {
        entity.HasKey(e => e.MessageId);
        entity.ToTable("Messages", "dbo");

        entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
        entity.HasIndex(e => new { e.SenderUserId, e.CreatedAt });
        entity.HasIndex(e => new { e.SenderGuestSessionId, e.CreatedAt });

        entity.Property(e => e.MessageId).HasColumnName("MessageID");
        entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
        entity.Property(e => e.SenderUserId).HasColumnName("SenderUserID");
        entity.Property(e => e.SenderGuestSessionId)
            .HasColumnName("SenderGuestSessionID")
            .HasMaxLength(64);
        entity.Property(e => e.Content);
        entity.Property(e => e.AttachmentUrl).HasMaxLength(1000);
        entity.Property(e => e.ReplyToMessageId).HasColumnName("ReplyToMessageID");
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(sysutcdatetime())");

        entity.HasOne(d => d.Conversation)
            .WithMany(p => p.Messages)
            .HasForeignKey(d => d.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(d => d.SenderUser)
            .WithMany()
            .HasForeignKey(d => d.SenderUserId);

        entity.HasOne(d => d.ReplyToMessage)
            .WithMany()
            .HasForeignKey(d => d.ReplyToMessageId);
    }
}
