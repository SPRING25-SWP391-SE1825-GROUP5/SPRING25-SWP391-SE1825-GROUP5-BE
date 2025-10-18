using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByBookingIdAsync(int bookingId);
        // GetByWorkOrderIdAsync removed - WorkOrder functionality merged into Booking
        Task<Invoice?> GetByOrderIdAsync(int orderId);
        Task<Invoice> CreateMinimalAsync(Invoice invoice);
        Task<Invoice?> GetByIdAsync(int invoiceId);
        Task<System.Collections.Generic.List<Invoice>> GetAllAsync();
        Task<System.Collections.Generic.List<Invoice>> GetByCustomerIdAsync(int customerId);
    }
}


