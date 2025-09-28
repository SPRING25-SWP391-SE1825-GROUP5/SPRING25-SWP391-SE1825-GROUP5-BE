using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateAllCentersScheduleRequest
    {
        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public TimeOnly EndTime { get; set; }


        public bool IsActive { get; set; } = true;
    }
}
