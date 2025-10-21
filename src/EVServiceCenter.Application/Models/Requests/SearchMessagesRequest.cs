using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class SearchMessagesRequest
    {
        [Required(ErrorMessage = "Từ khóa tìm kiếm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự")]
        public string SearchTerm { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "ID cuộc trò chuyện phải là số nguyên dương")]
        public long? ConversationId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID người dùng phải là số nguyên dương")]
        public int? UserId { get; set; }

        [StringLength(64, ErrorMessage = "ID phiên khách không được vượt quá 64 ký tự")]
        public string? GuestSessionId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
