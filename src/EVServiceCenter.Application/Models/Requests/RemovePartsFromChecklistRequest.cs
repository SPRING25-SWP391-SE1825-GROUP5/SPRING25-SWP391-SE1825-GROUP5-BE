using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class RemovePartsFromChecklistRequest
    {
        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải lớn hơn 0")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Danh sách Part IDs là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 Part ID")]
        public List<int> PartIds { get; set; } = new List<int>();
    }
}

