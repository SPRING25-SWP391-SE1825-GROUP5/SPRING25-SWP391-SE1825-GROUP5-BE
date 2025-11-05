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
        /// </summary>
        private async Task<StoreRevenueData> CalculateStoreRevenueAsync(
            int centerId,
            string centerName,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                // Lấy tất cả bookings của cửa hàng trong date range
                var allBookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId, 
                    page: 1, 
                    pageSize: int.MaxValue, 
                    status: null, 
                    fromDate: fromDate, 
                    toDate: toDate);

                // Lọc chỉ bookings đã hoàn thành (COMPLETED hoặc PAID)
                var completedBookings = allBookings.Where(b => 
                    !string.IsNullOrEmpty(b.Status) && 
                    (b.Status.ToUpperInvariant() == "COMPLETED" || b.Status.ToUpperInvariant() == "PAID")).ToList();

                decimal totalRevenue = 0;
                int completedCount = completedBookings.Count;

                // Tính doanh thu từ payments COMPLETED của các bookings này
                foreach (var booking in completedBookings)
                {
                    var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
                    if (invoice == null) continue;

                    // Lấy payments COMPLETED của invoice trong date range
                    var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: "COMPLETED",
                        method: null,
                        from: fromDate,
                        to: toDate);

                    // Tính tổng doanh thu từ payments
                    var paymentAmount = completedPayments
                        .Where(p => p.PaidAt >= fromDate && p.PaidAt <= toDate)
                        .Sum(p => p.Amount);

                    totalRevenue += paymentAmount;
                }

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

