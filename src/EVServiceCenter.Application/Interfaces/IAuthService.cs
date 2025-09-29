using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(AccountRequest request);
        Task<LoginTokenResponse> LoginAsync(LoginRequest request);
        Task<string> VerifyEmailAsync(int userId, string otpCode);
        Task<string> ResendVerificationEmailAsync(string email);
        Task<string> RequestResetPasswordAsync(string email);
        Task<string> ConfirmResetPasswordAsync(ConfirmResetPasswordRequest request);
        Task<string> LogoutAsync(int userId);
        Task<UserProfileResponse> GetUserProfileAsync(int userId);
        Task<string> UpdateUserProfileAsync(int userId, UpdateProfileRequest request);
        Task<string> UpdateUserAvatarAsync(int userId, string avatarUrl);
        Task<string> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<LoginTokenResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
        Task<bool> VerifyOtpAsync(string email, string otp);
        Task<string> SetPasswordWithTokenAsync(string token, string newPassword);
    }
}
