using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class PaymentMethodRevenueService : IPaymentMethodRevenueService
    {
        private readonly ICenterRepository _centerRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentMethodRevenueService> _logger;

        public PaymentMethodRevenueService(
            ICenterRepository centerRepository,
            IBookingRepository bookingRepository,
            IPaymentRepository paymentRepository,
            ILogger<PaymentMethodRevenueService> logger)
        {
            _centerRepository = centerRepository;
            _bookingRepository = bookingRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<PaymentMethodRevenueResponse> GetPaymentMethodRevenueAsync(
            int? centerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // Validate center if provided
                if (centerId.HasValue)
                {
                    var center = await _centerRepository.GetByIdAsync(centerId.Value);
                    if (center == null)
                    {
                        return new PaymentMethodRevenueResponse
                        {
                            Success = false,
                            Message = $"Không tìm thấy trung tâm với ID: {centerId}"
                        };
                    }
                }

                // Calculate date range
                var dateRange = CalculateDateRange(startDate, endDate);

                // Get all payments in date range
                var allPayments = await _paymentRepository.GetAllAsync();
                var filteredPayments = allPayments
                    .Where(p => p.CreatedAt >= dateRange.StartDate && p.CreatedAt <= dateRange.EndDate)
                    .ToList();

                // Filter by center if specified
                if (centerId.HasValue)
                {
                    // Get bookings for the center
                    var allBookings = await _bookingRepository.GetAllBookingsAsync();
                    var centerBookings = allBookings
                        .Where(b => b.CenterID == centerId.Value)
                        .Select(b => b.BookingId)
                        .ToHashSet();

                    // Get invoices for center bookings
                    var centerInvoices = filteredPayments
                        .Where(p => centerBookings.Contains(p.Invoice?.BookingId ?? 0))
                        .ToList();

                    filteredPayments = centerInvoices;
                }

                // Calculate revenue by payment method
                var payosPayments = filteredPayments.Where(p => p.PaymentMethod == "PAYOS").ToList();
                var cashPayments = filteredPayments.Where(p => p.PaymentMethod == "CASH").ToList();

                var payosRevenue = payosPayments.Sum(p => p.Amount);
                var cashRevenue = cashPayments.Sum(p => p.Amount);
                var totalRevenue = payosRevenue + cashRevenue;

                var payosCount = payosPayments.Count;
                var cashCount = cashPayments.Count;
                var totalCount = payosCount + cashCount;

                // Calculate percentages
                var payosPercentage = totalRevenue > 0 ? (payosRevenue / totalRevenue) * 100 : 0;
                var cashPercentage = totalRevenue > 0 ? (cashRevenue / totalRevenue) * 100 : 0;

                // Calculate averages
                var payosAverage = payosCount > 0 ? payosRevenue / payosCount : 0;
                var cashAverage = cashCount > 0 ? cashRevenue / cashCount : 0;
                var totalAverage = totalCount > 0 ? totalRevenue / totalCount : 0;

                // Get center name if specified
                string? centerName = null;
                if (centerId.HasValue)
                {
                    var center = await _centerRepository.GetByIdAsync(centerId.Value);
                    centerName = center?.CenterName;
                }

                var response = new PaymentMethodRevenueResponse
                {
                    Success = true,
                    Message = "Lấy doanh thu theo phương thức thanh toán thành công",
                    Data = new PaymentMethodRevenueData
                    {
                        CenterId = centerId,
                        CenterName = centerName,
                        PaymentMethods = new PaymentMethodsInfo
                        {
                            PAYOS = new PaymentMethodDetail
                            {
                                TotalRevenue = payosRevenue,
                                TransactionCount = payosCount,
                                Percentage = Math.Round(payosPercentage, 2),
                                AverageTransactionValue = Math.Round(payosAverage, 2)
                            },
                            CASH = new PaymentMethodDetail
                            {
                                TotalRevenue = cashRevenue,
                                TransactionCount = cashCount,
                                Percentage = Math.Round(cashPercentage, 2),
                                AverageTransactionValue = Math.Round(cashAverage, 2)
                            }
                        },
                        Summary = new RevenueSummary
                        {
                            TotalRevenue = totalRevenue,
                            TotalTransactions = totalCount,
                            AverageTransactionValue = Math.Round(totalAverage, 2)
                        },
                        DateRange = new DateRangeInfo
                        {
                            StartDate = dateRange.StartDate.ToString("yyyy-MM-dd"),
                            EndDate = dateRange.EndDate.ToString("yyyy-MM-dd")
                        },
                        LastUpdated = DateTime.UtcNow
                    }
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy doanh thu theo phương thức thanh toán cho center {CenterId}", centerId);
                return new PaymentMethodRevenueResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        private (DateTime StartDate, DateTime EndDate) CalculateDateRange(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-12); // Default 12 months ago
            var end = endDate ?? DateTime.Today;

            return (start, end);
        }
    }
}
