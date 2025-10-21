using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Models.Responses
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public required string Address { get; set; }
        public required string Gender { get; set; }
        public required string Role { get; set; }
        public required string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
    }
}
