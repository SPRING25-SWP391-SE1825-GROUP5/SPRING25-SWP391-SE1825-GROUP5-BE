using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class GetOrCreateConversationRequest
    {
        [Required(ErrorMessage = "Thành viên 1 là bắt buộc")]
        public ConversationMemberRequest Member1 { get; set; } = null!;

        [Required(ErrorMessage = "Thành viên 2 là bắt buộc")]
        public ConversationMemberRequest Member2 { get; set; } = null!;

        [StringLength(255, ErrorMessage = "Chủ đề cuộc trò chuyện không được vượt quá 255 ký tự")]
        public string? Subject { get; set; }

        public int? PreferredCenterId { get; set; }
    }

    public class ConversationMemberRequest
    {
        public int? UserId { get; set; }
        public string? GuestSessionId { get; set; }
        public string RoleInConversation { get; set; } = "CUSTOMER";
    }
}
