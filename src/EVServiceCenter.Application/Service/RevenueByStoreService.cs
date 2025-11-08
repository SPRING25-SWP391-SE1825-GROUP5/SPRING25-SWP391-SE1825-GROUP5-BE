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
        /// Sử dụng repository method tối ưu để lấy payments COMPLETED/PAID theo centerId và date range (theo PaidAt)
        /// Đảm bảo đồng nhất với DashboardSummaryService và ServiceBookingStatsService
        /// </summary>
        private async Task<StoreRevenueData> CalculateStoreRevenueAsync(
            int centerId,
            string centerName,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                // Sử dụng repository method tối ưu để lấy payments COMPLETED theo centerId và date range (theo PaidAt)
                var completedPayments = await _paymentRepository.GetCompletedPaymentsByCenterAndDateRangeAsync(
                    centerId, 
                    fromDate, 
                    toDate);

                // Lấy tất cả payments PAID trong date range với Invoice và Booking đã include
                // Sau đó filter theo centerId
                var statuses = new[] { "PAID" };
                var allPaidPayments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(
                    statuses, 
                    fromDate, 
                    toDate);

                // Filter payments PAID theo centerId thông qua Invoice -> Booking
                var paidPaymentsForCenter = allPaidPayments
                    .Where(p => p.Invoice != null 
                             && p.Invoice.BookingId != null 
                             && p.Invoice.Booking != null 
                             && p.Invoice.Booking.CenterId == centerId)
                    .ToList();

                // Tính tổng doanh thu từ payments COMPLETED và PAID
                var totalRevenue = completedPayments.Sum(p => (decimal)p.Amount) 
                                 + paidPaymentsForCenter.Sum(p => (decimal)p.Amount);

                // Đếm số booking đã hoàn thành: đếm số invoice unique có payments trong date range
                var completedInvoiceIds = completedPayments
                    .Select(p => p.InvoiceId)
                    .Concat(paidPaymentsForCenter.Select(p => p.InvoiceId))
                    .Distinct()
                    .ToList();

                var completedCount = completedInvoiceIds.Count;

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

