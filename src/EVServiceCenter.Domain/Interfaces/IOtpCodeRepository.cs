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
        Task<Otpcode?> GetLastOtpCodeAsync(int userId, string type);
        Task<Otpcode> CreateOtpCodeAsync(Otpcode otp);
        Task UpdateAsync(Otpcode otp);
    }
}
