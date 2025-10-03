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

        // TechnicianCode removed

        [Required(ErrorMessage = "Vị trí là bắt buộc")]
        [StringLength(100, ErrorMessage = "Vị trí không được vượt quá 100 ký tự")]
        public string Position { get; set; }

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
