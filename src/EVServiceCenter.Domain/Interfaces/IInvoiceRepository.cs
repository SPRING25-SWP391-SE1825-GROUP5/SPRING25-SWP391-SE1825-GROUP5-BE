using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByBookingIdAsync(int bookingId);
        Task<Invoice> CreateMinimalAsync(Invoice invoice);
        Task CreateInvoiceItemsAsync(List<InvoiceItem> invoiceItems);
    }
}


