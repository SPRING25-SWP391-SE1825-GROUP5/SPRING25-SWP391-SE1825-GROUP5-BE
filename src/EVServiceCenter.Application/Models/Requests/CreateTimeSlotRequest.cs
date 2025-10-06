using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateTimeSlotRequest
    {
        [Required(ErrorMessage = "Thời gian slot là bắt buộc")]
        public TimeOnly SlotTime { get; set; }

        [Required(ErrorMessage = "Nhãn slot là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Nhãn slot phải từ 2-50 ký tự")]
        public string SlotLabel { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateTimeSlotRequest
    {
        [Required(ErrorMessage = "Thời gian slot là bắt buộc")]
        public TimeOnly SlotTime { get; set; }

        [Required(ErrorMessage = "Nhãn slot là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Nhãn slot phải từ 2-50 ký tự")]
        public string SlotLabel { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
