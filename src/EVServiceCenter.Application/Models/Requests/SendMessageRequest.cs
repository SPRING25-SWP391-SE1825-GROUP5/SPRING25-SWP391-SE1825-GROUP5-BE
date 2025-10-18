using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class SendMessageRequest
    {
        [Required(ErrorMessage = "ID cuộc trò chuyện là bắt buộc")]
        [Range(1, long.MaxValue, ErrorMessage = "ID cuộc trò chuyện phải là số nguyên dương")]
        public long ConversationId { get; set; }

        [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc")]
        [StringLength(4000, ErrorMessage = "Nội dung tin nhắn không được vượt quá 4000 ký tự")]
        public string Content { get; set; } = string.Empty;

        public int? SenderUserId { get; set; }
        public string? SenderGuestSessionId { get; set; }

        [StringLength(1000, ErrorMessage = "URL đính kèm không được vượt quá 1000 ký tự")]
        public string? AttachmentUrl { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "ID tin nhắn trả lời phải là số nguyên dương")]
        public long? ReplyToMessageId { get; set; }
    }
}
