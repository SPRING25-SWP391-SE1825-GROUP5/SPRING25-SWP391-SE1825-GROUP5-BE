using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
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
            // Deprecated: PayOSOrderCode column removed. No-op and return null.
            await Task.CompletedTask;
            return null;
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

        public async Task<List<Payment>> GetByInvoiceIdAsync(int invoiceId, string? status = null, string? method = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _db.Payments.AsQueryable().Where(p => p.InvoiceId == invoiceId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
            if (!string.IsNullOrWhiteSpace(method)) query = query.Where(p => p.PaymentMethod == method);
            // Filter by PaidAt (doanh thu thực nhận) thay vì CreatedAt
            if (from.HasValue) query = query.Where(p => p.PaidAt != null && p.PaidAt >= from.Value);
            if (to.HasValue) query = query.Where(p => p.PaidAt != null && p.PaidAt <= to.Value);
            return await query.OrderBy(p => p.PaidAt).ToListAsync();
        }

        public async Task<List<Payment>> GetCompletedPaymentsByCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate)
        {
            // Tối ưu query: join Payment -> Invoice -> Booking trong một query để tránh N+1
            // Lọc payments theo status COMPLETED, PaidAt trong khoảng thời gian, và centerId thông qua Booking
            var payments = await _db.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                .Where(p => p.Status == "COMPLETED" 
                         && p.PaidAt != null 
                         && p.PaidAt >= fromDate 
                         && p.PaidAt <= toDate
                         && p.Invoice != null 
                         && p.Invoice.BookingId != null
                         && p.Invoice.Booking != null 
                         && p.Invoice.Booking.CenterId == centerId)
                .OrderBy(p => p.PaidAt)
                .ToListAsync();

            return payments;
        }

        public async Task<List<Payment>> GetCompletedPaymentsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            // Lấy tất cả payments COMPLETED trong khoảng PaidAt và include Invoice (để dùng PartsAmount nếu cần)
            return await _db.Payments
                .Include(p => p.Invoice)
                .Where(p => p.Status == "COMPLETED"
                         && p.PaidAt != null
                         && p.PaidAt >= fromDate
                         && p.PaidAt <= toDate)
                .OrderBy(p => p.PaidAt)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetPaymentsByDateRangeAsync(string status, DateTime fromDate, DateTime toDate)
        {
            status = status.Trim();
            return await _db.Payments
                .Include(p => p.Invoice)
                .Where(p => p.Status == status
                         && p.PaidAt != null
                         && p.PaidAt >= fromDate
                         && p.PaidAt <= toDate)
                .OrderBy(p => p.PaidAt)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetPaymentsByStatusesAndDateRangeAsync(IEnumerable<string> statuses, DateTime fromDate, DateTime toDate)
        {
            var statusSet = statuses.Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return await _db.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                        .ThenInclude(b => b.Service)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Order)
                .Where(p => statusSet.Contains(p.Status)
                         && p.PaidAt != null
                         && p.PaidAt >= fromDate
                         && p.PaidAt <= toDate)
                .OrderBy(p => p.PaidAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy payments COMPLETED hoặc PAID từ orders (e-commerce) theo FulfillmentCenterId và khoảng thời gian PaidAt
        /// </summary>
        public async Task<List<Payment>> GetCompletedPaymentsByFulfillmentCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate)
        {
            // Tối ưu query: join Payment -> Invoice -> Order trong một query để tránh N+1
            // Lọc payments theo status COMPLETED hoặc PAID, PaidAt trong khoảng thời gian, và centerId thông qua Order.FulfillmentCenterId
            var payments = await _db.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Order)
                .Where(p => (p.Status == "COMPLETED" || p.Status == "PAID")
                         && p.PaidAt != null 
                         && p.PaidAt >= fromDate 
                         && p.PaidAt <= toDate
                         && p.Invoice != null 
                         && p.Invoice.OrderId != null
                         && p.Invoice.Order != null 
                         && p.Invoice.Order.FulfillmentCenterId == centerId)
                .OrderBy(p => p.PaidAt)
                .ToListAsync();

            return payments;
        }

        /// <summary>
        /// Lấy payments PAID từ orders (e-commerce) theo FulfillmentCenterId và khoảng thời gian PaidAt
        /// </summary>
        public async Task<List<Payment>> GetPaidPaymentsByFulfillmentCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate)
        {
            // Tối ưu query: join Payment -> Invoice -> Order trong một query để tránh N+1
            // Lọc payments theo status PAID, PaidAt trong khoảng thời gian, và centerId thông qua Order.FulfillmentCenterId
            var payments = await _db.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Order)
                .Where(p => p.Status == "PAID" 
                         && p.PaidAt != null 
                         && p.PaidAt >= fromDate 
                         && p.PaidAt <= toDate
                         && p.Invoice != null 
                         && p.Invoice.OrderId != null
                         && p.Invoice.Order != null 
                         && p.Invoice.Order.FulfillmentCenterId == centerId)
                .OrderBy(p => p.PaidAt)
                .ToListAsync();

            return payments;
        }
    }
}


