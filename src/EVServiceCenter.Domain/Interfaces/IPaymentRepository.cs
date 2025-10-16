using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByPayOsOrderCodeAsync(long payOsOrderCode);
        Task<Payment> CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task<int> CountByInvoiceIdAsync(int invoiceId);
        Task<List<Payment>> GetByInvoiceIdAsync(int invoiceId, string? status = null, string? method = null, DateTime? from = null, DateTime? to = null);
    }
}


