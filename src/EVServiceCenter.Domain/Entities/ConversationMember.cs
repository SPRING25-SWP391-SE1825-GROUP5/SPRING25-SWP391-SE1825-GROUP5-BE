using System;

namespace EVServiceCenter.Domain.Entities;

public class ConversationMember
{
    public long MemberId { get; set; }
    public long ConversationId { get; set; }
    public int? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public string RoleInConversation { get; set; } = string.Empty; // CUSTOMER | STAFF | ADMIN | MANAGER | GUEST
    public DateTime? LastReadAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User? User { get; set; }
}


