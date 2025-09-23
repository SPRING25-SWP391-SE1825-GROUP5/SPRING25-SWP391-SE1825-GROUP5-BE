using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly EVDbContext _db;
        public PaymentRepository(EVDbContext db) { _db = db; }

        public async Task<Payment?> GetByPayOsOrderCodeAsync(long payOsOrderCode)
        {
            return await _db.Payments.FirstOrDefaultAsync(p => p.PayOsorderCode == payOsOrderCode);
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            return payment;
        }

        public async Task UpdateAsync(Payment payment)
        {
            _db.Payments.Update(payment);
            await _db.SaveChangesAsync();
        }
    }
}


