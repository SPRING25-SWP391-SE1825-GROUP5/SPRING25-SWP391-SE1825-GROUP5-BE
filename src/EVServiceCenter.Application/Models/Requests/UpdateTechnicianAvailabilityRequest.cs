using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateTechnicianAvailabilityRequest
    {
        [Required(ErrorMessage = "Ngày làm việc là bắt buộc")]
        public DateOnly WorkDate { get; set; }

        [Required(ErrorMessage = "Danh sách time slots là bắt buộc")]
        public required List<TimeSlotAvailabilityUpdate> TimeSlots { get; set; }
    }

    public class TimeSlotAvailabilityUpdate
    {
        [Required(ErrorMessage = "Slot ID là bắt buộc")]
        public int SlotId { get; set; }

        [Required(ErrorMessage = "Trạng thái available là bắt buộc")]
        public bool IsAvailable { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public required string Notes { get; set; }
    }
}
