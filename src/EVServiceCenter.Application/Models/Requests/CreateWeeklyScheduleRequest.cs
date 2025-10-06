using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateWeeklyScheduleRequest
    {
        [Required(ErrorMessage = "Center ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Center ID phải lớn hơn 0")]
        public int CenterId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Technician ID phải lớn hơn 0")]
        public int? TechnicianId { get; set; }

        [Required(ErrorMessage = "Day of Week là bắt buộc")]
        [Range(0, 6, ErrorMessage = "Day of Week phải từ 0 đến 6 (Chủ nhật đến Thứ bảy)")]
        public byte DayOfWeek { get; set; }

        [Required(ErrorMessage = "Is Open là bắt buộc")]
        public bool IsOpen { get; set; } = true;

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }

        public TimeOnly? BreakStart { get; set; }

        public TimeOnly? BreakEnd { get; set; }

        [Range(0, 255, ErrorMessage = "Buffer Minutes phải từ 0 đến 255")]
        public byte BufferMinutes { get; set; } = 10;

        [Range(1, 255, ErrorMessage = "Step Minutes phải từ 1 đến 255")]
        public byte StepMinutes { get; set; } = 30;

        [Required(ErrorMessage = "Effective From là bắt buộc")]
        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public DateOnly? EffectiveTo { get; set; }

        [Required(ErrorMessage = "Is Active là bắt buộc")]
        public bool IsActive { get; set; } = true;

        [StringLength(300, ErrorMessage = "Notes không được vượt quá 300 ký tự")]
        public string? Notes { get; set; }
    }
}

