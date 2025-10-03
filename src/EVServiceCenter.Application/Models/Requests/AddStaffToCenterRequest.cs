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
    }
}
