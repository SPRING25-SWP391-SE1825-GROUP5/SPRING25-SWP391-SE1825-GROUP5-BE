using System.Threading.Tasks;
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
    }
}


