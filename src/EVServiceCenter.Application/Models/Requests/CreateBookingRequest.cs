using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "ID khách hàng là bắt buộc")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "ID phương tiện là bắt buộc")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "ID trung tâm là bắt buộc")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Ngày đặt lịch là bắt buộc")]
        [DataType(DataType.Date, ErrorMessage = "Ngày đặt lịch không đúng định dạng YYYY-MM-DD")]
        public DateOnly BookingDate { get; set; }

        [Required(ErrorMessage = "ID slot bắt đầu là bắt buộc")]
        public int StartSlotId { get; set; }

        [Required(ErrorMessage = "ID slot kết thúc là bắt buộc")]
        public int EndSlotId { get; set; }

        [StringLength(500, ErrorMessage = "Yêu cầu đặc biệt không được vượt quá 500 ký tự")]
        public string SpecialRequests { get; set; }

        [Required(ErrorMessage = "Danh sách dịch vụ là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 dịch vụ")]
        public List<BookingServiceRequest> Services { get; set; } = new List<BookingServiceRequest>();
    }

}
