using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class OtpCodeRepository : IOtpCodeRepository
    {
        private readonly EVDbContext _context;
        public OtpCodeRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<Otpcode?> GetLastOtpCodeAsync(int userId, string type)
        {
            return  _context.Otpcodes.Where(o => o.UserId == userId && o.Otptype == type)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();
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
    }
}
