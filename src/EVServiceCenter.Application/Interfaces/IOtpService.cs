using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IOtpService
    {
        /// <summary>
        /// Tạo mã OTP ngẫu nhiên
        /// </summary>
        string GenerateOtp(int length = 6);

        /// <summary>
        /// Tạo và lưu OTP cho user
        /// </summary>
        Task<string> CreateOtpAsync(int userId, string email, string otpType = "EMAIL_VERIFICATION");

        /// <summary>
        /// Xác thực OTP
        /// </summary>
        Task<bool> VerifyOtpAsync(int userId, string otpCode, string otpType = "EMAIL_VERIFICATION");

        /// <summary>
        /// Tăng số lần thử OTP sai
        /// </summary>
        Task IncrementAttemptCountAsync(int userId, string otpCode, string otpType = "EMAIL_VERIFICATION");

        /// <summary>
        /// Kiểm tra xem user có thể tạo OTP mới không
        /// </summary>
        Task<bool> CanCreateNewOtpAsync(int userId, string otpType = "EMAIL_VERIFICATION");
    }
}
