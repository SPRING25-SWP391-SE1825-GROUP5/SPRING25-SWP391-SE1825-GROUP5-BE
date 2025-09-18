using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class LoginTokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } // seconds
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool EmailVerified { get; set; }
    }
}
