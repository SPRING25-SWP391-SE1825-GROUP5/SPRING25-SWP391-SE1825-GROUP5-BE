using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateTechnicianRequest
    {
        [StringLength(20, ErrorMessage = "Mã kỹ thuật viên không được vượt quá 20 ký tự")]
        public string TechnicianCode { get; set; }

        [StringLength(200, ErrorMessage = "Chuyên môn không được vượt quá 200 ký tự")]
        public string Specialization { get; set; }

        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 50")]
        public int? ExperienceYears { get; set; }

        public bool? IsActive { get; set; }
    }
}
