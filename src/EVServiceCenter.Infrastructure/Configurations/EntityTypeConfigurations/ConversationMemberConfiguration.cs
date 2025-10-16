using EVServiceCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EVServiceCenter.Infrastructure.Configurations.EntityTypeConfigurations;

public sealed class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> entity)
    {
        entity.HasKey(e => e.MemberId);
        entity.ToTable("ConversationMembers", "dbo");

        entity.HasIndex(e => new { e.ConversationId, e.UserId }).HasDatabaseName("UX_ConversationMembers_Conversation_User");
        entity.HasIndex(e => new { e.ConversationId, e.GuestSessionId }).HasDatabaseName("UX_ConversationMembers_Conversation_Guest");
        entity.HasIndex(e => e.UserId).HasDatabaseName("IX_ConversationMembers_User");
        entity.HasIndex(e => e.GuestSessionId).HasDatabaseName("IX_ConversationMembers_Guest");

        entity.Property(e => e.MemberId).HasColumnName("MemberID");
        entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
        entity.Property(e => e.UserId).HasColumnName("UserID");
        entity.Property(e => e.GuestSessionId).HasColumnName("GuestSessionID").HasMaxLength(64);
        entity.Property(e => e.RoleInConversation).HasMaxLength(16);
        entity.Property(e => e.LastReadAt);

        entity.HasOne(d => d.Conversation)
            .WithMany() // no navigation collection configured on Conversation
            .HasForeignKey(d => d.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId);
    }
}


