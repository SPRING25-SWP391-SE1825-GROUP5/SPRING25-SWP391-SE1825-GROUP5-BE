using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddMemberToConversationRequest
    {
        public int? UserId { get; set; }
        public string? GuestSessionId { get; set; }

        [Required(ErrorMessage = "Vai trò trong cuộc trò chuyện là bắt buộc")]
        [StringLength(16, ErrorMessage = "Vai trò không được vượt quá 16 ký tự")]
        public string RoleInConversation { get; set; } = "CUSTOMER";
    }
}
