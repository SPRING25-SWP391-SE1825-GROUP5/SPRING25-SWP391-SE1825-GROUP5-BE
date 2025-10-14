using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IOtpCodeRepository
    {
        /// <summary>
        /// Tạo OTP mới
        /// </summary>
        Task CreateOtpAsync(Otpcode otp);

        /// <summary>
        /// Lấy OTP hợp lệ
        /// </summary>
        Task<Otpcode> GetValidOtpAsync(int userId, string otpCode, string otpType);

        /// <summary>
        /// Lấy OTP cuối cùng của user
        /// </summary>
        Task<Otpcode> GetLastOtpAsync(int userId, string otpType);

        /// <summary>
        /// Đánh dấu OTP đã sử dụng
        /// </summary>
        Task MarkOtpAsUsedAsync(int otpId);

        /// <summary>
        /// Đánh dấu OTP hết hạn
        /// </summary>
        Task MarkOtpAsExpiredAsync(int otpId);

        /// <summary>
        /// Vô hiệu hóa tất cả OTP cũ của user
        /// </summary>
        Task InvalidateUserOtpAsync(int userId, string otpType);

        /// <summary>
        /// Tăng số lần thử
        /// </summary>
        Task IncrementAttemptCountAsync(int userId, string otpCode, string otpType);

        // Legacy methods for backward compatibility
        Task<Otpcode?> GetLastOtpCodeAsync(int userId, string type);
        Task<Otpcode> CreateOtpCodeAsync(Otpcode otp);
        Task UpdateAsync(Otpcode otp);
        Task<Otpcode?> GetByRawTokenAsync(string token);
    }
}
