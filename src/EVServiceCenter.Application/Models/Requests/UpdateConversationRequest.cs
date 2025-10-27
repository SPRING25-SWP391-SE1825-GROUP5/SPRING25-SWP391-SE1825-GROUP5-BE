using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateConversationRequest
    {
        [StringLength(255, ErrorMessage = "Chủ đề cuộc trò chuyện không được vượt quá 255 ký tự")]
        public string? Subject { get; set; }
    }
}
