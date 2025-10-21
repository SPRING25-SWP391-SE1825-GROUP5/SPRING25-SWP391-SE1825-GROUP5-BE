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
        /// T?o OTP m?i
        /// </summary>
        Task CreateOtpAsync(Otpcode otp);

        /// <summary>
        /// L?y OTP h?p l?
        /// </summary>
        Task<Otpcode?> GetValidOtpAsync(int userId, string otpCode, string otpType);

        /// <summary>
        /// L?y OTP cu?i c�ng c?a user
        /// </summary>
        Task<Otpcode?> GetLastOtpAsync(int userId, string otpType);

        /// <summary>
        /// ��nh d?u OTP d� s? d?ng
        /// </summary>
        Task MarkOtpAsUsedAsync(int otpId);

        /// <summary>
        /// ��nh d?u OTP h?t h?n
        /// </summary>
        Task MarkOtpAsExpiredAsync(int otpId);

        /// <summary>
        /// V� hi?u h�a t?t c? OTP cu c?a user
        /// </summary>
        Task InvalidateUserOtpAsync(int userId, string otpType);

        /// <summary>
        /// Tang s? l?n th?
        /// </summary>
        Task IncrementAttemptCountAsync(int userId, string otpCode, string otpType);

        // Legacy methods for backward compatibility
        Task<Otpcode?> GetLastOtpCodeAsync(int userId, string type);
        Task<Otpcode> CreateOtpCodeAsync(Otpcode otp);
        Task UpdateAsync(Otpcode otp);
        Task<Otpcode?> GetByRawTokenAsync(string token);
    }
}
