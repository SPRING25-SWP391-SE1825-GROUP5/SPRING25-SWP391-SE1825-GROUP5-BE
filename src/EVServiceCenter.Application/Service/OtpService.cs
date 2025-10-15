using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class OtpService : IOtpService
    {
        private readonly IOtpCodeRepository _otpRepository;

        public OtpService(IOtpCodeRepository otpRepository)
        {
            _otpRepository = otpRepository;
        }

        /// <summary>
        /// Tạo mã OTP 6 chữ số ngẫu nhiên
        /// </summary>
        public string GenerateOtp(int length = 6)
        {
            var random = new Random();
            var otp = "";
            for (int i = 0; i < length; i++)
            {
                otp += random.Next(0, 10).ToString();
            }
            return otp;
        }

        /// <summary>
        /// Tạo và lưu OTP cho user
        /// </summary>
        public async Task<string> CreateOtpAsync(int userId, string email, string otpType = "EMAIL_VERIFICATION")
        {
            // Hủy tất cả OTP cũ chưa sử dụng của user cho loại này
            await _otpRepository.InvalidateUserOtpAsync(userId, otpType);

            // Tạo OTP mới
            var otpCode = GenerateOtp();
            var otp = new Otpcode
            {
                UserId = userId,
                Otpcode1 = otpCode,
                Otptype = otpType,
                ContactInfo = email,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15), // Hết hạn sau 15 phút
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _otpRepository.CreateOtpAsync(otp);
            return otpCode;
        }

        /// <summary>
        /// Xác thực OTP
        /// </summary>
        public async Task<bool> VerifyOtpAsync(int userId, string otpCode, string otpType = "EMAIL_VERIFICATION")
        {
            var otp = await _otpRepository.GetValidOtpAsync(userId, otpCode, otpType);
            
            if (otp == null)
                return false;

            // Kiểm tra hết hạn
            if (otp.ExpiresAt < DateTime.UtcNow)
            {
                await _otpRepository.MarkOtpAsExpiredAsync(otp.Otpid);
                return false;
            }

            // Kiểm tra số lần thử (giới hạn 5 lần)
            if (otp.AttemptCount >= 5)
            {
                await _otpRepository.MarkOtpAsExpiredAsync(otp.Otpid);
                return false;
            }

            // OTP hợp lệ - đánh dấu đã sử dụng
            await _otpRepository.MarkOtpAsUsedAsync(otp.Otpid);
            return true;
        }

        /// <summary>
        /// Tăng số lần thử OTP sai
        /// </summary>
        public async Task IncrementAttemptCountAsync(int userId, string otpCode, string otpType = "EMAIL_VERIFICATION")
        {
            await _otpRepository.IncrementAttemptCountAsync(userId, otpCode, otpType);
        }

        /// <summary>
        /// Kiểm tra xem user có thể tạo OTP mới không (chống spam)
        /// </summary>
        public async Task<bool> CanCreateNewOtpAsync(int userId, string otpType = "EMAIL_VERIFICATION")
        {
            var lastOtp = await _otpRepository.GetLastOtpAsync(userId, otpType);
            if (lastOtp == null)
                return true;

            // Chỉ cho phép tạo OTP mới sau 2 phút
            return lastOtp.CreatedAt.AddMinutes(2) < DateTime.UtcNow;
        }
    }
}
