using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AssignBookingTimeSlotsRequest
    {
        [Required(ErrorMessage = "Danh sách time slots là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 time slot")]
        public List<BookingTimeSlotRequest> TimeSlots { get; set; } = new List<BookingTimeSlotRequest>();
    }

    public class BookingTimeSlotRequest
    {
        [Required(ErrorMessage = "ID slot là bắt buộc")]
        public int SlotId { get; set; }

        [Required(ErrorMessage = "ID kỹ thuật viên là bắt buộc")]
        public int TechnicianId { get; set; }

        [Required(ErrorMessage = "Thứ tự slot là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự slot phải lớn hơn 0")]
        public int SlotOrder { get; set; }
    }
}
