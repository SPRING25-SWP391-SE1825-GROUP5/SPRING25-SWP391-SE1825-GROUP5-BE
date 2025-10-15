using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly EVDbContext _context;

        public OtpRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task CreateOtpAsync(Otpcode otp)
        {
            await _context.Otpcodes.AddAsync(otp);
            await _context.SaveChangesAsync();
        }

        public async Task<Otpcode> GetValidOtpAsync(int userId, string otpCode, string otpType)
        {
            return await _context.Otpcodes
                .FirstOrDefaultAsync(o => o.UserId == userId 
                                    && o.Otpcode1 == otpCode 
                                    && o.Otptype == otpType 
                                    && !o.IsUsed 
                                    && o.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<Otpcode> GetLastOtpAsync(int userId, string otpType)
        {
            return await _context.Otpcodes
                .Where(o => o.UserId == userId && o.Otptype == otpType)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

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

        public async Task MarkOtpAsExpiredAsync(int otpId)
        {
            var otp = await _context.Otpcodes.FindAsync(otpId);
            if (otp != null)
            {
                otp.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // Đặt thời gian hết hạn về quá khứ
                _context.Otpcodes.Update(otp);
                await _context.SaveChangesAsync();
            }
        }

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
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            if (otps.Any())
            {
                _context.Otpcodes.UpdateRange(otps);
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncrementAttemptCountAsync(int userId, string otpCode, string otpType)
        {
            var otp = await _context.Otpcodes
                .FirstOrDefaultAsync(o => o.UserId == userId 
                                    && o.Otpcode1 == otpCode 
                                    && o.Otptype == otpType 
                                    && !o.IsUsed);

            if (otp != null)
            {
                otp.AttemptCount++;
                _context.Otpcodes.Update(otp);
                await _context.SaveChangesAsync();
            }
        }
    }
}
