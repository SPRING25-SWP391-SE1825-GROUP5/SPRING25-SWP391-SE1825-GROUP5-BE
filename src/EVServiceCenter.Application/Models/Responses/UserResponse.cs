using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class UserResponse
    {
        public int UserId { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public required string Address { get; set; }
        public required string Gender { get; set; }
        public required string AvatarUrl { get; set; }
        public required string Role { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }
}
