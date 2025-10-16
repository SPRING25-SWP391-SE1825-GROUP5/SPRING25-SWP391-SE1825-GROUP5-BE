using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddPartsToChecklistRequest
    {
        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải lớn hơn 0")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Danh sách Parts là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 Part")]
        public required List<PartData> Parts { get; set; } = new List<PartData>();

        public class PartData
        {
            [Required(ErrorMessage = "ID Part là bắt buộc")]
            [Range(1, int.MaxValue, ErrorMessage = "ID Part phải lớn hơn 0")]
            public int PartId { get; set; }

            [StringLength(200, ErrorMessage = "Ghi chú không được vượt quá 200 ký tự")]
            public string? Notes { get; set; }
        }
    }
}
