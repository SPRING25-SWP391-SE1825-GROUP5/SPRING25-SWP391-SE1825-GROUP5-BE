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

        public async Task<(List<Payment> Items, int TotalCount)> QueryForAdminAsync(
            int page,
            int pageSize,
            int? customerId = null,
            int? invoiceId = null,
            int? bookingId = null,
            int? orderId = null,
            string? status = null,
            string? paymentMethod = null,
            DateTime? from = null,
            DateTime? to = null,
            string? searchTerm = null,
            string sortBy = "createdAt",
            string sortOrder = "desc")
        {
            var q = _db.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Customer)
                        .ThenInclude(c => c.User)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Order)
                .AsQueryable();

            // Apply filters
            if (customerId.HasValue)
                q = q.Where(p => p.Invoice != null && p.Invoice.CustomerId == customerId.Value);

            if (invoiceId.HasValue)
                q = q.Where(p => p.InvoiceId == invoiceId.Value);

            if (bookingId.HasValue)
                q = q.Where(p => p.Invoice != null && p.Invoice.BookingId == bookingId.Value);

            if (orderId.HasValue)
                q = q.Where(p => p.Invoice != null && p.Invoice.OrderId == orderId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                q = q.Where(p => p.Status == s);
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                var method = paymentMethod.Trim().ToUpperInvariant();
                q = q.Where(p => p.PaymentMethod == method);
            }

            if (from.HasValue)
                q = q.Where(p => p.CreatedAt >= from.Value || (p.PaidAt != null && p.PaidAt >= from.Value));

            if (to.HasValue)
                q = q.Where(p => p.CreatedAt <= to.Value || (p.PaidAt != null && p.PaidAt <= to.Value));

            // Search term - search by PaymentCode, Customer name, InvoiceId
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                q = q.Where(p =>
                    (p.PaymentCode != null && p.PaymentCode.ToLower().Contains(search)) ||
                    (p.Invoice != null && p.Invoice.Customer != null && p.Invoice.Customer.User != null &&
                     p.Invoice.Customer.User.FullName != null && p.Invoice.Customer.User.FullName.ToLower().Contains(search)) ||
                    (p.Invoice != null && p.InvoiceId.ToString().Contains(search))
                );
            }

            // Get total count before pagination
            var totalCount = await q.CountAsync();

            // Apply sorting
            var isDescending = sortOrder?.ToLowerInvariant() == "desc";
            switch (sortBy?.ToLowerInvariant())
            {
                case "paidat":
                    q = isDescending ? q.OrderByDescending(p => p.PaidAt ?? p.CreatedAt) : q.OrderBy(p => p.PaidAt ?? p.CreatedAt);
                    break;
                case "amount":
                    q = isDescending ? q.OrderByDescending(p => p.Amount) : q.OrderBy(p => p.Amount);
                    break;
                case "status":
                    q = isDescending ? q.OrderByDescending(p => p.Status) : q.OrderBy(p => p.Status);
                    break;
                case "paymentmethod":
                    q = isDescending ? q.OrderByDescending(p => p.PaymentMethod) : q.OrderBy(p => p.PaymentMethod);
                    break;
                case "createdat":
                default:
                    q = isDescending ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt);
                    break;
            }

            // Apply pagination
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Payment?> GetByIdWithDetailsAsync(int paymentId)
        {
            return await _db.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Customer)
                        .ThenInclude(c => c.User)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                        .ThenInclude(b => b.Service)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                        .ThenInclude(b => b.Customer)
                            .ThenInclude(c => c.User)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Order)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }
    }
}


