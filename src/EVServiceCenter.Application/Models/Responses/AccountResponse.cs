using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AccountResponse
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public required string Address { get; set; }
        public required string Role { get; set; }
    }
}
