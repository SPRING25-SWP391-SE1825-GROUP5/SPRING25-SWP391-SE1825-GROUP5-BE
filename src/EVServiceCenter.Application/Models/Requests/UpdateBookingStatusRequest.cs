using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateBookingStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(PENDING|CONFIRMED|CHECKED_IN|IN_PROGRESS|COMPLETED|PAID|CANCELLED)$",
            ErrorMessage = "Trạng thái phải là PENDING, CONFIRMED, CHECKED_IN, IN_PROGRESS, COMPLETED, PAID hoặc CANCELLED")]
        public required string Status { get; set; }
    }
}
