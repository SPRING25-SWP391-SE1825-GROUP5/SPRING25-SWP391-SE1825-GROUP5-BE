using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class MessageResponse
    {
        public long MessageId { get; set; }
        public long ConversationId { get; set; }
        public int? SenderUserId { get; set; }
        public string? SenderGuestSessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public long? ReplyToMessageId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Sender information
        public string? SenderName { get; set; }
        public string? SenderEmail { get; set; }
        public string? SenderAvatar { get; set; }
        public bool IsGuest { get; set; }
        
        // Reply information
        public MessageResponse? ReplyToMessage { get; set; }
        public List<MessageResponse> Replies { get; set; } = new List<MessageResponse>();
    }
}
