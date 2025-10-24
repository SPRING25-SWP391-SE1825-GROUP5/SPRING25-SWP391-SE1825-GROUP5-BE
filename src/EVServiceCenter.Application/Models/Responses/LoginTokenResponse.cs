using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class LoginTokenResponse
    {
        public required string AccessToken { get; set; }
        public required string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } // seconds
        public DateTime ExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public int? StaffId { get; set; }
        public int? TechnicianId { get; set; }
        public required string FullName { get; set; }
        public required string Role { get; set; }
        public bool EmailVerified { get; set; }
        
        // Thêm các field cần thiết cho FE
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
