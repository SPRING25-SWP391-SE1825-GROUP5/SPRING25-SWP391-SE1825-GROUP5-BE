using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AddTechnicianToCenterRequest : IValidatableObject
    {
        // Gán kỹ thuật viên đã tồn tại vào center: dùng TechnicianId
        public int TechnicianId { get; set; }

        [Required(ErrorMessage = "ID trung tâm là bắt buộc")]
        public int CenterId { get; set; }

        // TechnicianCode removed

        [Required(ErrorMessage = "Vị trí là bắt buộc")]
        [StringLength(100, ErrorMessage = "Vị trí không được vượt quá 100 ký tự")]
        public string Position { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TechnicianId <= 0)
            {
                yield return new ValidationResult("TechnicianId là bắt buộc và phải > 0.", new[] { nameof(TechnicianId) });
            }
        }
    }
}
