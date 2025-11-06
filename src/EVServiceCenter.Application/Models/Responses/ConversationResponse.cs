using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class ConversationResponse
    {
        public long ConversationId { get; set; }
        public string? Subject { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public long? LastMessageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }
        public string? AssignedStaffEmail { get; set; }
        public int? AssignedCenterId { get; set; }
        public string? AssignedCenterName { get; set; }

        public MessageResponse? LastMessage { get; set; }
        public List<ConversationMemberResponse> Members { get; set; } = new List<ConversationMemberResponse>();
        public List<MessageResponse> Messages { get; set; } = new List<MessageResponse>();
    }
}
