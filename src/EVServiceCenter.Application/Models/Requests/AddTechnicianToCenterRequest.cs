using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddTechnicianToCenterRequest
    {
        [Required(ErrorMessage = "ID người dùng là bắt buộc")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "ID trung tâm là bắt buộc")]
        public int CenterId { get; set; }

        [Required(ErrorMessage = "Mã kỹ thuật viên là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã kỹ thuật viên không được vượt quá 20 ký tự")]
        public string TechnicianCode { get; set; }

        [Required(ErrorMessage = "Chuyên môn là bắt buộc")]
        [StringLength(200, ErrorMessage = "Chuyên môn không được vượt quá 200 ký tự")]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "Số năm kinh nghiệm là bắt buộc")]
        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 50")]
        public int ExperienceYears { get; set; }
    }
}
