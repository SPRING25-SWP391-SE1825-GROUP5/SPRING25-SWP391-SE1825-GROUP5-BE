using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateBookingFeedbackRequest
    {
        [Required(ErrorMessage = "CustomerId là bắt buộc")]
        public int CustomerId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment không được quá 500 ký tự")]
        public string? Comment { get; set; }

        public bool IsAnonymous { get; set; } = false;

        // Optional - có thể đánh giá technician và/hoặc part
        public int? TechnicianId { get; set; }
        public int? PartId { get; set; }
    }
}
