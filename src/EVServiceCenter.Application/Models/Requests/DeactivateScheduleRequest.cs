using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class DeactivateScheduleRequest
    {
        [Required(ErrorMessage = "Center ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Center ID phải lớn hơn 0")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Thứ bắt đầu là bắt buộc")]
        [Range(1, 6, ErrorMessage = "Thứ bắt đầu phải từ 1 (Thứ 2) đến 6 (Thứ 7)")]
        public byte StartDayOfWeek { get; set; }

        [Required(ErrorMessage = "Thứ kết thúc là bắt buộc")]
        [Range(1, 6, ErrorMessage = "Thứ kết thúc phải từ 1 (Thứ 2) đến 6 (Thứ 7)")]
        public byte EndDayOfWeek { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public TimeOnly EndTime { get; set; }

        public bool IsActive { get; set; } = false; // false = deactivate, true = reactivate
    }
}
