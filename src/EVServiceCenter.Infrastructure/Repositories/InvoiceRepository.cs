using System.Threading.Tasks;
using System.Collections.Generic;
using EVServiceCenter.Domain.Configurations;
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
            return await _db.Invoices.FirstOrDefaultAsync(i => i.WorkOrder != null && i.WorkOrder.BookingId == bookingId);
        }

        public async Task<Invoice> CreateMinimalAsync(Invoice invoice)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice;
        }

        // InvoiceItems removed – no-op placeholder to keep interface compatibility if used elsewhere
        public async Task CreateInvoiceItemsAsync(System.Collections.Generic.List<object> invoiceItems)
        {
            await Task.CompletedTask;
        }

        public async Task<Invoice?> GetByIdAsync(int invoiceId)
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.WorkOrder)
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }
    }
}


