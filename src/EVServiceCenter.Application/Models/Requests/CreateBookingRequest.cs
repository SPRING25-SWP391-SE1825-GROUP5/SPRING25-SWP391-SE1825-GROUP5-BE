using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "ID khách hàng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID khách hàng phải là số nguyên dương")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "ID phương tiện là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID phương tiện phải là số nguyên dương")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "ID trung tâm là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID trung tâm phải là số nguyên dương")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Ngày đặt lịch là bắt buộc")]
        [DataType(DataType.Date, ErrorMessage = "Ngày đặt lịch không đúng định dạng YYYY-MM-DD")]
        public DateOnly BookingDate { get; set; }

        [Required(ErrorMessage = "ID slot là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID slot phải là số nguyên dương")]
        public int SlotId { get; set; }

        [StringLength(500, ErrorMessage = "Yêu cầu đặc biệt không được vượt quá 500 ký tự")]
        public string SpecialRequests { get; set; }

        // Mô hình 1 booking = 1 service
        [Required(ErrorMessage = "ID dịch vụ là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID dịch vụ phải là số nguyên dương")]
        public int ServiceId { get; set; }

        // Optional: cho phép chọn kỹ thuật viên. Nếu null, hệ thống sẽ auto-assign
        public int? TechnicianId { get; set; }
    }

}
