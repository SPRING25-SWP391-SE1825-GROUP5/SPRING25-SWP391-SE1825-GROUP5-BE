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

        [Required(ErrorMessage = "Ngày hiệu lực từ là bắt buộc")]
        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public DateOnly? EffectiveTo { get; set; }

        [Required(ErrorMessage = "Tổng capacity là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "CapacityTotal phải lớn hơn 0")]
        public int CapacityTotal { get; set; }

        [Required(ErrorMessage = "Capacity còn lại là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "CapacityLeft phải lớn hơn hoặc bằng 0")]
        public int CapacityLeft { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
