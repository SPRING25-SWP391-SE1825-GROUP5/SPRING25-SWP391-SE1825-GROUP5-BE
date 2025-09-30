using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<int> CountByInvoiceIdAsync(int invoiceId)
        {
            return await _db.Payments.CountAsync(p => p.InvoiceId == invoiceId);
        }

        public async Task<List<Payment>> GetByInvoiceIdAsync(int invoiceId, string status = null, string method = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _db.Payments.AsQueryable().Where(p => p.InvoiceId == invoiceId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
            if (!string.IsNullOrWhiteSpace(method)) query = query.Where(p => p.PaymentMethod == method);
            if (from.HasValue) query = query.Where(p => p.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.CreatedAt <= to.Value);
            return await query.OrderBy(p => p.CreatedAt).ToListAsync();
        }
    }
}


