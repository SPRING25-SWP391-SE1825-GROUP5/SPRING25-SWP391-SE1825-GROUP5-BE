using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddStaffToCenterRequest
    {
        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "ID trung tâm là bắt buộc")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã nhân viên không được vượt quá 20 ký tự")]
        public string StaffCode { get; set; }

        [Required(ErrorMessage = "Vị trí là bắt buộc")]
        [StringLength(100, ErrorMessage = "Vị trí không được vượt quá 100 ký tự")]
        public string Position { get; set; }

        [Required(ErrorMessage = "Ngày tuyển dụng là bắt buộc")]
        [DataType(DataType.Date, ErrorMessage = "Ngày tuyển dụng không đúng định dạng YYYY-MM-DD")]
        public DateOnly HireDate { get; set; }
    }
}
