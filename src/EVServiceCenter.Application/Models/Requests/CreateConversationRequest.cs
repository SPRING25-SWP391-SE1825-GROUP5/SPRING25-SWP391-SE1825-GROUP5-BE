using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateConversationRequest
    {
        [StringLength(255, ErrorMessage = "Chủ đề cuộc trò chuyện không được vượt quá 255 ký tự")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Danh sách thành viên là bắt buộc")]
        [MinLength(1, ErrorMessage = "Cuộc trò chuyện phải có ít nhất 1 thành viên")]
        public List<AddMemberToConversationRequest> Members { get; set; } = new List<AddMemberToConversationRequest>();

        public int? PreferredCenterId { get; set; }

        public decimal? CustomerLatitude { get; set; }

        public decimal? CustomerLongitude { get; set; }
    }
}
