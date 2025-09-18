using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AssignBookingServicesRequest
    {
        [Required(ErrorMessage = "Danh sách dịch vụ là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 dịch vụ")]
        public List<BookingServiceRequest> Services { get; set; } = new List<BookingServiceRequest>();
    }

    public class BookingServiceRequest
    {
        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }
}
