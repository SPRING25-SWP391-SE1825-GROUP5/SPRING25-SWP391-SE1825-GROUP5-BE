using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly EVDbContext _db;
        public InvoiceRepository(EVDbContext db) { _db = db; }

        public async Task<Invoice?> GetByBookingIdAsync(int bookingId)
        {
            return await _db.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Service)
                .FirstOrDefaultAsync(i => i.BookingId == bookingId);
        }

        // GetByWorkOrderIdAsync removed - WorkOrder functionality merged into Booking

        public async Task<Invoice?> GetByOrderIdAsync(int orderId)
        {
            return await _db.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
        }

        public async Task<Invoice> CreateMinimalAsync(Invoice invoice)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateAmountsAsync(int invoiceId, decimal packageDiscountAmount, decimal promotionDiscountAmount, decimal partsAmount)
        {
            // Tránh OUTPUT clause khi có trigger: dùng UPDATE thủ công
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE [dbo].[Invoices]
                SET [PackageDiscountAmount] = {packageDiscountAmount},
                    [PromotionDiscountAmount] = {promotionDiscountAmount},
                    [PartsAmount] = {partsAmount}
                WHERE [InvoiceID] = {invoiceId}
            ");
        }


        public async Task<Invoice?> GetByIdAsync(int invoiceId)
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                // WorkOrder removed - functionality merged into Booking
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                // WorkOrder removed - functionality merged into Booking
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Service)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByCustomerIdAsync(int customerId)
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                // WorkOrder removed - functionality merged into Booking
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Service)
                .Where(i => i.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(int invoiceId, string status)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE [dbo].[Invoices]
                SET [Status] = {status}
                WHERE [InvoiceID] = {invoiceId}
            ");
        }

        public async Task<(List<Invoice> Items, int TotalCount)> QueryForAdminAsync(
            int page,
            int pageSize,
            int? customerId = null,
            int? bookingId = null,
            int? orderId = null,
            string? status = null,
            DateTime? from = null,
            DateTime? to = null,
            string? searchTerm = null,
            string sortBy = "createdAt",
            string sortOrder = "desc")
        {
            var q = _db.Invoices
                .Include(i => i.Customer)
                    .ThenInclude(c => c.User)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Service)
                .Include(i => i.Order)
                .Include(i => i.Payments)
                .AsQueryable();

            // Apply filters
            if (customerId.HasValue)
                q = q.Where(i => i.CustomerId == customerId.Value);

            if (bookingId.HasValue)
                q = q.Where(i => i.BookingId == bookingId.Value);

            if (orderId.HasValue)
                q = q.Where(i => i.OrderId == orderId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                q = q.Where(i => i.Status == s);
            }

            if (from.HasValue)
                q = q.Where(i => i.CreatedAt >= from.Value);

            if (to.HasValue)
                q = q.Where(i => i.CreatedAt <= to.Value);

            // Search term - search by InvoiceId, Customer name, Email, Phone
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                q = q.Where(i =>
                    i.InvoiceId.ToString().Contains(search) ||
                    (i.Customer != null && i.Customer.User != null &&
                     i.Customer.User.FullName != null && i.Customer.User.FullName.ToLower().Contains(search)) ||
                    (i.Email != null && i.Email.ToLower().Contains(search)) ||
                    (i.Phone != null && i.Phone.ToLower().Contains(search))
                );
            }

            // Get total count before pagination
            var totalCount = await q.CountAsync();

            // Apply sorting
            var isDescending = sortOrder?.ToLowerInvariant() == "desc";
            switch (sortBy?.ToLowerInvariant())
            {
                case "status":
                    q = isDescending ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status);
                    break;
                case "createdat":
                default:
                    q = isDescending ? q.OrderByDescending(i => i.CreatedAt) : q.OrderBy(i => i.CreatedAt);
                    break;
            }

            // Apply pagination
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Invoice?> GetByIdWithDetailsAsync(int invoiceId)
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                    .ThenInclude(c => c.User)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Service)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                        .ThenInclude(c => c.User)
                .Include(i => i.Order)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }
    }
}


