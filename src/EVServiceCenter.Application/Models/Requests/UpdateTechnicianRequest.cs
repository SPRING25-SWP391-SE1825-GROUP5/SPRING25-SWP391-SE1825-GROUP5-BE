using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateTechnicianRequest
    {
        // TechnicianCode removed

        [StringLength(100, ErrorMessage = "Vị trí không được vượt quá 100 ký tự")]
        public required string Position { get; set; }

        public bool? IsActive { get; set; }
    }
}
