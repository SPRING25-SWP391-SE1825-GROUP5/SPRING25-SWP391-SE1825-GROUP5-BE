using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateStaffRequest
    {
        [StringLength(20, ErrorMessage = "Mã nhân viên không được vượt quá 20 ký tự")]
        public string StaffCode { get; set; }

        [StringLength(100, ErrorMessage = "Vị trí không được vượt quá 100 ký tự")]
        public string Position { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Ngày tuyển dụng không đúng định dạng YYYY-MM-DD")]
        public DateOnly? HireDate { get; set; }

        public bool? IsActive { get; set; }
    }
}
