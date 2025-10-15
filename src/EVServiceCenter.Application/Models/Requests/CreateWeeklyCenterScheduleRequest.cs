using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateWeeklyCenterScheduleRequest
    {
        [Required(ErrorMessage = "Center ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Center ID phải lớn hơn 0")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public TimeOnly EndTime { get; set; }



        public bool IsActive { get; set; } = true;
    }
}
