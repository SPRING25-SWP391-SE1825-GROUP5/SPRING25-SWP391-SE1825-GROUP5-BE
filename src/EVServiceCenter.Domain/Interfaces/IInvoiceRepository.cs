using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByBookingIdAsync(int bookingId);
        Task<Invoice?> GetByWorkOrderIdAsync(int workOrderId);
        Task<Invoice?> GetByOrderIdAsync(int orderId);
        Task<Invoice> CreateMinimalAsync(Invoice invoice);
        // Legacy no-op to keep compatibility with old calls
        Task CreateInvoiceItemsAsync(System.Collections.Generic.List<object> invoiceItems);
        Task<Invoice?> GetByIdAsync(int invoiceId);
        Task<System.Collections.Generic.List<Invoice>> GetAllAsync();
        Task<System.Collections.Generic.List<Invoice>> GetByCustomerIdAsync(int customerId);
    }
}


