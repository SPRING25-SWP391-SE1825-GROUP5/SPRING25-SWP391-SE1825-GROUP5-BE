using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class UserResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }
    }
}
