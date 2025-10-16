using System;

namespace EVServiceCenter.Domain.Entities;

public class Message
{
    public long MessageId { get; set; }
    public long ConversationId { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderGuestSessionId { get; set; }
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public long? ReplyToMessageId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public User? SenderUser { get; set; }
    public Message? ReplyToMessage { get; set; }
}


