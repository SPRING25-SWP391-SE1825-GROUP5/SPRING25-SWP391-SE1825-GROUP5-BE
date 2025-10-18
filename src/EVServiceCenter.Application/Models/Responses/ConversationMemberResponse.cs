using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ConversationMemberResponse
    {
        public long MemberId { get; set; }
        public long ConversationId { get; set; }
        public int? UserId { get; set; }
        public string? GuestSessionId { get; set; }
        public string RoleInConversation { get; set; } = string.Empty;
        public DateTime? LastReadAt { get; set; }
        
        // User information if available
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserAvatar { get; set; }
    }
}
