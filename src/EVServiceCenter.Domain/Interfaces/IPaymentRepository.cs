using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByPayOsOrderCodeAsync(long payOsOrderCode);
        Task<Payment> CreateAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task<int> CountByInvoiceIdAsync(int invoiceId);
        Task<List<Payment>> GetByInvoiceIdAsync(int invoiceId, string? status = null, string? method = null, DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Lấy payments đã thanh toán (COMPLETED) theo centerId và khoảng thời gian PaidAt
        /// Tối ưu query bằng cách join Payment -> Invoice -> Booking trong một query
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="fromDate">Ngày bắt đầu (filter theo PaidAt)</param>
        /// <param name="toDate">Ngày kết thúc (filter theo PaidAt)</param>
        /// <returns>Danh sách payments với Invoice và Booking đã include</returns>
        Task<List<Payment>> GetCompletedPaymentsByCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy tất cả payments COMPLETED trong khoảng thời gian PaidAt (toàn hệ thống)
        /// </summary>
        Task<List<Payment>> GetCompletedPaymentsByDateRangeAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy tất cả payments theo trạng thái và khoảng thời gian PaidAt (toàn hệ thống)
        /// </summary>
        Task<List<Payment>> GetPaymentsByDateRangeAsync(string status, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy tất cả payments theo danh sách trạng thái và khoảng thời gian PaidAt (toàn hệ thống)
        /// </summary>
        Task<List<Payment>> GetPaymentsByStatusesAndDateRangeAsync(IEnumerable<string> statuses, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy payments từ orders (e-commerce) theo FulfillmentCenterId và khoảng thời gian PaidAt
        /// Tối ưu query bằng cách join Payment -> Invoice -> Order trong một query
        /// </summary>
        /// <param name="centerId">ID trung tâm (FulfillmentCenterId)</param>
        /// <param name="fromDate">Ngày bắt đầu (filter theo PaidAt)</param>
        /// <param name="toDate">Ngày kết thúc (filter theo PaidAt)</param>
        /// <returns>Danh sách payments với Invoice và Order đã include</returns>
        Task<List<Payment>> GetCompletedPaymentsByFulfillmentCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Lấy payments PAID từ orders (e-commerce) theo FulfillmentCenterId và khoảng thời gian PaidAt
        /// </summary>
        /// <param name="centerId">ID trung tâm (FulfillmentCenterId)</param>
        /// <param name="fromDate">Ngày bắt đầu (filter theo PaidAt)</param>
        /// <param name="toDate">Ngày kết thúc (filter theo PaidAt)</param>
        /// <returns>Danh sách payments với Invoice và Order đã include</returns>
        Task<List<Payment>> GetPaidPaymentsByFulfillmentCenterAndDateRangeAsync(int centerId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Admin: Query payments với pagination, filter, search, sort
        /// </summary>
        Task<(List<Payment> Items, int TotalCount)> QueryForAdminAsync(
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
            string sortOrder = "desc");

        /// <summary>
        /// Admin: Lấy payment với đầy đủ thông tin (Invoice, Customer, Booking, Order)
        /// </summary>
        Task<Payment?> GetByIdWithDetailsAsync(int paymentId);
    }
}


