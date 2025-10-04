using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class LoginLockoutConfigRequest
    {
        [Required]
        [Range(1, 20, ErrorMessage = "MaxFailedAttempts phải từ 1 đến 20")]
        public int MaxFailedAttempts { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "LockoutDurationMinutes phải từ 1 đến 1440 phút (24 giờ)")]
        public int LockoutDurationMinutes { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "CacheKeyPrefix không được quá 50 ký tự")]
        public string CacheKeyPrefix { get; set; } = string.Empty;

        [Required]
        public bool Enabled { get; set; }
    }
}
