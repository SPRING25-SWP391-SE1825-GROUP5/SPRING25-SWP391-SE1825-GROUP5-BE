using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddTechnicianToCenterRequest : IValidatableObject
    {
        // Chế độ 1 (tạo mới): dùng UserId
        public int? UserId { get; set; }

        // Chế độ 2 (gán kỹ thuật viên đã tồn tại vào center): dùng TechnicianId
        public int? TechnicianId { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasUser = UserId.HasValue && UserId.Value > 0;
            var hasTech = TechnicianId.HasValue && TechnicianId.Value > 0;

            if (hasUser == hasTech)
            {
                yield return new ValidationResult("Cần cung cấp đúng 1 trong 2: userId hoặc technicianId.", new[] { nameof(UserId), nameof(TechnicianId) });
            }
        }
    }
}
