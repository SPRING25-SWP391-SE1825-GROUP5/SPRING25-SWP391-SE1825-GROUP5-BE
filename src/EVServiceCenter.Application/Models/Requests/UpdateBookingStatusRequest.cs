using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateBookingStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(PENDING|CONFIRMED|IN_PROGRESS|COMPLETED|CANCELLED)$", 
            ErrorMessage = "Trạng thái phải là PENDING, CONFIRMED, IN_PROGRESS, COMPLETED hoặc CANCELLED")]
        public required string Status { get; set; }
    }
}
