using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public class Conversation
{
    public long ConversationId { get; set; }
    public string? Subject { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public long? LastMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Message? LastMessage { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
}


