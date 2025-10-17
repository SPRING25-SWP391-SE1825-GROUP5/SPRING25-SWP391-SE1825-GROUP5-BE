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
            return await _db.Invoices.FirstOrDefaultAsync(i => i.WorkOrder != null && i.WorkOrder.BookingId == bookingId);
        }

        public async Task<Invoice?> GetByWorkOrderIdAsync(int workOrderId)
        {
            return await _db.Invoices.FirstOrDefaultAsync(i => i.WorkOrderId == workOrderId);
        }

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


        public async Task<Invoice?> GetByIdAsync(int invoiceId)
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.WorkOrder)
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.WorkOrder)
                .Include(i => i.Booking)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByCustomerIdAsync(int customerId)
        {
            return await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.WorkOrder)
                .Include(i => i.Booking)
                .Where(i => i.CustomerId == customerId)
                .ToListAsync();
        }
    }
}


