using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public class Conversation
{
    public long ConversationId { get; set; }
    public int? CustomerId { get; set; }
    public string? Subject { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN | RESOLVED | CLOSED
    public DateTime LastMessageAt { get; set; }
    public long? LastMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
    public Message? LastMessage { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}


