using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateWeeklyTimeSlotRequest
    {
        [Required(ErrorMessage = "CenterId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "CenterId phải lớn hơn 0")]
        public int CenterId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TechnicianId phải lớn hơn 0")]
        public int? TechnicianId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateOnly EndDate { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public TimeOnly EndTime { get; set; }

        public TimeOnly? BreakStart { get; set; }

        public TimeOnly? BreakEnd { get; set; }

        [Required(ErrorMessage = "BufferMinutes là bắt buộc")]
        [Range(0, 255, ErrorMessage = "BufferMinutes phải từ 0 đến 255")]
        public byte BufferMinutes { get; set; } = 10;

        [Required(ErrorMessage = "StepMinutes là bắt buộc")]
        [Range(1, 255, ErrorMessage = "StepMinutes phải từ 1 đến 255")]
        public byte StepMinutes { get; set; } = 30;

        [Required(ErrorMessage = "Ngày hiệu lực từ là bắt buộc")]
        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public DateOnly? EffectiveTo { get; set; }

        [Required(ErrorMessage = "IsActive là bắt buộc")]
        public bool IsActive { get; set; } = true;

        [StringLength(300, ErrorMessage = "Notes không được vượt quá 300 ký tự")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Danh sách ngày trong tuần là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 ngày trong tuần")]
        public required List<byte> DaysOfWeek { get; set; } = new List<byte>();

        // Validation method
        public bool IsValid()
        {
            if (StartDate >= EndDate)
                return false;

            if (StartTime >= EndTime)
                return false;

            if (BreakStart.HasValue && BreakEnd.HasValue)
            {
                if (BreakStart >= BreakEnd)
                    return false;
                
                if (BreakStart <= StartTime || BreakEnd >= EndTime)
                    return false;
            }

            if (DaysOfWeek.Any(day => day < 0 || day > 6))
                return false;

            return true;
        }
    }
}
