using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class AssignUserRoleRequest
    {
        [Required(ErrorMessage = "User ID là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID phải là số dương")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [RegularExpression(@"^(ADMIN|STAFF|TECHNICIAN|CUSTOMER)$", ErrorMessage = "Vai trò phải là ADMIN, STAFF, TECHNICIAN hoặc CUSTOMER")]
        public required string Role { get; set; }
    }
}
