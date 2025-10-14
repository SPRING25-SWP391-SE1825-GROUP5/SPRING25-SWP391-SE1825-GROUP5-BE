using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class OtpCodeRepository : IOtpCodeRepository
    {
        private readonly EVDbContext _context;
        public OtpCodeRepository(EVDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tạo OTP mới
        /// </summary>
        public async Task CreateOtpAsync(Otpcode otp)
        {
            await _context.Otpcodes.AddAsync(otp);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy OTP hợp lệ
        /// </summary>
        public async Task<Otpcode> GetValidOtpAsync(int userId, string otpCode, string otpType)
        {
            return await _context.Otpcodes
                .FirstOrDefaultAsync(o => o.UserId == userId 
                                    && o.Otpcode1 == otpCode 
                                    && o.Otptype == otpType 
                                    && !o.IsUsed 
                                    && o.ExpiresAt > DateTime.UtcNow);
        }

        /// <summary>
        /// Lấy OTP cuối cùng của user
        /// </summary>
        public async Task<Otpcode> GetLastOtpAsync(int userId, string otpType)
        {
            return await _context.Otpcodes
                .Where(o => o.UserId == userId && o.Otptype == otpType)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Đánh dấu OTP đã sử dụng
        /// </summary>
        public async Task MarkOtpAsUsedAsync(int otpId)
        {
            var otp = await _context.Otpcodes.FindAsync(otpId);
            if (otp != null)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
                _context.Otpcodes.Update(otp);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Đánh dấu OTP hết hạn
        /// </summary>
        public async Task MarkOtpAsExpiredAsync(int otpId)
        {
            var otp = await _context.Otpcodes.FindAsync(otpId);
            if (otp != null)
            {
                otp.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // Set to past time
                _context.Otpcodes.Update(otp);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Vô hiệu hóa tất cả OTP cũ của user
        /// </summary>
        public async Task InvalidateUserOtpAsync(int userId, string otpType)
        {
            var otps = await _context.Otpcodes
                .Where(o => o.UserId == userId 
                       && o.Otptype == otpType 
                       && !o.IsUsed 
                       && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // Set to past time
            }

            if (otps.Any())
            {
                _context.Otpcodes.UpdateRange(otps);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Tăng số lần thử
        /// </summary>
        public async Task IncrementAttemptCountAsync(int userId, string otpCode, string otpType)
        {
            var otp = await _context.Otpcodes
                .FirstOrDefaultAsync(o => o.UserId == userId 
                                    && o.Otpcode1 == otpCode 
                                    && o.Otptype == otpType);
            if (otp != null)
            {
                otp.AttemptCount++;
                _context.Otpcodes.Update(otp);
                await _context.SaveChangesAsync();
            }
        }

        // Legacy methods for backward compatibility
        public Task<Otpcode?> GetLastOtpCodeAsync(int userId, string type)
        {
            return Task.FromResult(_context.Otpcodes.Where(o => o.UserId == userId && o.Otptype == type)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault());
        }

        public async Task<Otpcode> CreateOtpCodeAsync(Otpcode otp)
        {
            _context.Otpcodes.Add(otp);
            await _context.SaveChangesAsync();
            return otp;
        }

        public async Task UpdateAsync(Otpcode otp)
        {
            _context.Otpcodes.Update(otp);
            await _context.SaveChangesAsync();
        }

        public Task<Otpcode?> GetByRawTokenAsync(string token)
        {
            // In this system, we store OTP codes in Otpcode.Otpcode1; we'll match directly
            return Task.FromResult(_context.Otpcodes
                .Where(o => o.Otpcode1 == token)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault());
        }
    }
}
