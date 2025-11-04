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
        
        /// <summary>
        /// Lấy payments đã thanh toán (COMPLETED) theo centerId và khoảng thời gian PaidAt
        /// Tối ưu query bằng cách join Payment -> Invoice -> Booking trong một query
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="fromDate">Ngày bắt đầu (filter theo PaidAt)</param>
        /// <param name="toDate">Ngày kết thúc (filter theo PaidAt)</param>
        /// <returns>Danh sách payments với Invoice và Booking đã include</returns>
        Task<List<Payment>> GetCompletedPaymentsByCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate);
    }
}


