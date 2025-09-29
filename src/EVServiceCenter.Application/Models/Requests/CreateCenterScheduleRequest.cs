using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateCenterScheduleRequest
    {
        [Required(ErrorMessage = "Center ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Center ID phải lớn hơn 0")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Ngày trong tuần là bắt buộc")]
        [Range(0, 6, ErrorMessage = "DayOfWeek phải từ 0-6 (Chủ nhật đến Thứ bảy)")]
        public byte DayOfWeek { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public TimeOnly EndTime { get; set; }

        // SlotLength removed: fixed 30 minutes system-wide



        public DateOnly? ScheduleDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
