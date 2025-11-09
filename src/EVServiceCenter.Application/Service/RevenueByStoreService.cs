using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    /// <summary>
    /// Service implementation cho Revenue by Store - So sánh doanh thu giữa các cửa hàng
    /// </summary>
    public class RevenueByStoreService : IRevenueByStoreService
    {
        private readonly ICenterRepository _centerRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<RevenueByStoreService> _logger;

        public RevenueByStoreService(
            ICenterRepository centerRepository,
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository,
            ILogger<RevenueByStoreService> logger)
        {
            _centerRepository = centerRepository;
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        /// <summary>
        /// Lấy doanh thu của tất cả cửa hàng để so sánh theo date range
        /// </summary>
        public async Task<RevenueByStoreResponse> GetRevenueByStoreAsync(RevenueByStoreRequest? request = null)
        {
            try
            {
                // Set default values nếu không có request
                var fromDate = request?.FromDate ?? DateTime.Today.AddDays(-30);
                var toDate = request?.ToDate ?? DateTime.Today;

                // Normalize dates
                fromDate = fromDate.Date;
                toDate = toDate.Date.AddDays(1).AddTicks(-1); // End of day

                _logger.LogInformation(
                    "Bắt đầu tính toán Revenue by Store từ {FromDate} đến {ToDate}",
                    fromDate, toDate);

                // Lấy tất cả cửa hàng (centers)
                var allCenters = await _centerRepository.GetAllCentersAsync();
                
                // Chỉ lấy các cửa hàng đang active
                var activeCenters = allCenters.Where(c => c.IsActive).ToList();

                if (!activeCenters.Any())
                {
                    _logger.LogWarning("Không có cửa hàng nào đang hoạt động");
                    return new RevenueByStoreResponse
                    {
                        Success = true,
                        GeneratedAt = DateTime.UtcNow,
                        FromDate = fromDate,
                        ToDate = toDate,
                        Stores = new List<StoreRevenueData>(),
                        TotalRevenue = 0
                    };
                }

                var storeRevenues = new List<StoreRevenueData>();
                decimal totalRevenueAllStores = 0;

                // Tính doanh thu cho từng cửa hàng
                foreach (var center in activeCenters)
                {
                    var storeRevenue = await CalculateStoreRevenueAsync(center.CenterId, center.CenterName, 
                        fromDate, toDate);
                    
                    storeRevenues.Add(storeRevenue);
                    totalRevenueAllStores += storeRevenue.Revenue;
                }

                // Sắp xếp theo doanh thu giảm dần
                storeRevenues = storeRevenues.OrderByDescending(s => s.Revenue).ToList();

                _logger.LogInformation(
                    "Revenue by Store tính toán thành công: {StoreCount} cửa hàng, Tổng doanh thu: {TotalRevenue}",
                    storeRevenues.Count, totalRevenueAllStores);

                return new RevenueByStoreResponse
                {
                    Success = true,
                    GeneratedAt = DateTime.UtcNow,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Stores = storeRevenues,
                    TotalRevenue = totalRevenueAllStores
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính toán Revenue by Store");
                throw;
            }
        }

        /// <summary>
        /// Tính doanh thu cho một cửa hàng cụ thể
        /// Tổng doanh thu = Booking revenue + Order revenue
        /// - Booking revenue: từ payments của bookings có CenterId = centerId
        /// - Order revenue: từ payments của orders có FulfillmentCenterId = centerId
        /// </summary>
        private async Task<StoreRevenueData> CalculateStoreRevenueAsync(
            int centerId,
            string centerName,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                // ========== BOOKING REVENUE ==========
                // Lấy payments COMPLETED từ bookings của center này
                var completedBookingPayments = await _paymentRepository.GetCompletedPaymentsByCenterAndDateRangeAsync(
                    centerId, 
                    fromDate, 
                    toDate);

                // Lấy payments PAID từ bookings của center này
                var statuses = new[] { "PAID" };
                var allPaidPayments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(
                    statuses, 
                    fromDate, 
                    toDate);

                // Filter payments PAID từ bookings theo centerId
                var paidBookingPayments = allPaidPayments
                    .Where(p => p.Invoice != null 
                             && p.Invoice.BookingId != null 
                             && p.Invoice.Booking != null 
                             && p.Invoice.Booking.CenterId == centerId)
                    .ToList();

                // Tính booking revenue
                var bookingRevenue = completedBookingPayments.Sum(p => (decimal)p.Amount) 
                                   + paidBookingPayments.Sum(p => (decimal)p.Amount);

                // ========== ORDER REVENUE ==========
                // Lấy payments COMPLETED hoặc PAID từ orders có FulfillmentCenterId = centerId
                // Note: GetCompletedPaymentsByFulfillmentCenterAndDateRangeAsync đã lấy cả COMPLETED và PAID
                var orderPayments = await _paymentRepository.GetCompletedPaymentsByFulfillmentCenterAndDateRangeAsync(
                    centerId, 
                    fromDate, 
                    toDate);

                // Tính order revenue
                var orderRevenue = orderPayments.Sum(p => (decimal)p.Amount);

                // ========== TỔNG DOANH THU ==========
                // Tổng doanh thu = Booking revenue + Order revenue
                var totalRevenue = bookingRevenue + orderRevenue;

                // Đếm số booking đã hoàn thành: đếm số invoice unique có payments từ bookings
                var completedBookingInvoiceIds = completedBookingPayments
                    .Select(p => p.InvoiceId)
                    .Concat(paidBookingPayments.Select(p => p.InvoiceId))
                    .Distinct()
                    .ToList();

                var completedCount = completedBookingInvoiceIds.Count;

                _logger.LogInformation(
                    "Tính doanh thu cho center {CenterId}: Booking revenue = {BookingRevenue}, Order revenue = {OrderRevenue}, Total = {TotalRevenue}",
                    centerId, bookingRevenue, orderRevenue, totalRevenue);

                return new StoreRevenueData
                {
                    StoreId = centerId,
                    StoreName = centerName,
                    Revenue = totalRevenue,
                    CompletedBookings = completedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính doanh thu cho cửa hàng {CenterId}", centerId);
                throw;
            }
        }
    }
}

