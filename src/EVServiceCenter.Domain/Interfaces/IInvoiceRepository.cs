using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByBookingIdAsync(int bookingId);
        Task<List<Invoice>> GetByBookingIdsAsync(List<int> bookingIds);
        // GetByWorkOrderIdAsync removed - WorkOrder functionality merged into Booking
        Task<Invoice?> GetByOrderIdAsync(int orderId);
        Task<Invoice> CreateMinimalAsync(Invoice invoice);
        Task UpdateAmountsAsync(int invoiceId, decimal packageDiscountAmount, decimal promotionDiscountAmount, decimal partsAmount);
        Task<Invoice?> GetByIdAsync(int invoiceId);
        Task<System.Collections.Generic.List<Invoice>> GetAllAsync();
        Task<System.Collections.Generic.List<Invoice>> GetByCustomerIdAsync(int customerId);
        Task UpdateStatusAsync(int invoiceId, string status);

        /// <summary>
        /// Admin: Query invoices với pagination, filter, search, sort
        /// </summary>
        Task<(System.Collections.Generic.List<Invoice> Items, int TotalCount)> QueryForAdminAsync(
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
            string sortOrder = "desc");

        /// <summary>
        /// Admin: Lấy invoice với đầy đủ thông tin (Customer, Booking, Order, Payments)
        /// </summary>
        Task<Invoice?> GetByIdWithDetailsAsync(int invoiceId);
    }
}


