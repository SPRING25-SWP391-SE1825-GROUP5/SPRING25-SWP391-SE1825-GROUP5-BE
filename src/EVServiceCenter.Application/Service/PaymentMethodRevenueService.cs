using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class PaymentMethodRevenueService : IPaymentMethodRevenueService
    {
        private readonly ICenterRepository _centerRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IPaymentRepository _paymentRepository;

        public PaymentMethodRevenueService(
            ICenterRepository centerRepository,
            IBookingRepository bookingRepository,
            IPaymentRepository paymentRepository)
        {
            _centerRepository = centerRepository;
            _bookingRepository = bookingRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<PaymentMethodRevenueResponse> GetPaymentMethodRevenueAsync(
            int? centerId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                if (centerId.HasValue)
                {
                    var center = await _centerRepository.GetCenterByIdAsync(centerId.Value);
                    if (center == null)
                    {
                        return new PaymentMethodRevenueResponse
                        {
                            Success = false,
                            Message = $"Không tìm thấy trung tâm với ID: {centerId}"
                        };
                    }
                }

                var dateRange = CalculateDateRange(startDate, endDate);

                var allPayments = await _paymentRepository.GetByInvoiceIdAsync(0);
                var filteredPayments = allPayments
                    .Where(p => p.CreatedAt >= dateRange.StartDate && p.CreatedAt <= dateRange.EndDate)
                    .ToList();

                if (centerId.HasValue)
                {
                    var allBookings = await _bookingRepository.GetAllBookingsAsync();
                    var centerBookings = allBookings
                        .Where(b => b.CenterId == centerId.Value)
                        .Select(b => b.BookingId)
                        .ToHashSet();

                    var centerInvoices = filteredPayments
                        .Where(p => centerBookings.Contains(p.Invoice?.BookingId ?? 0))
                        .ToList();

                    filteredPayments = centerInvoices;
                }

                var payosPayments = filteredPayments.Where(p => p.PaymentMethod == "PAYOS").ToList();
                var cashPayments = filteredPayments.Where(p => p.PaymentMethod == "CASH").ToList();

                var payosRevenue = payosPayments.Sum(p => p.Amount);
                var cashRevenue = cashPayments.Sum(p => p.Amount);
                var totalRevenue = payosRevenue + cashRevenue;

                var payosCount = payosPayments.Count;
                var cashCount = cashPayments.Count;
                var totalCount = payosCount + cashCount;

                var payosPercentage = totalRevenue > 0 ? (payosRevenue / totalRevenue) * 100 : 0;
                var cashPercentage = totalRevenue > 0 ? (cashRevenue / totalRevenue) * 100 : 0;

                var payosAverage = payosCount > 0 ? payosRevenue / payosCount : 0;
                var cashAverage = cashCount > 0 ? cashRevenue / cashCount : 0;
                var totalAverage = totalCount > 0 ? totalRevenue / totalCount : 0;

                string? centerName = null;
                if (centerId.HasValue)
                {
                    var center = await _centerRepository.GetCenterByIdAsync(centerId.Value);
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
                                Percentage = (decimal)Math.Round((double)payosPercentage, 2),
                                AverageTransactionValue = (decimal)Math.Round((double)payosAverage, 2)
                            },
                            CASH = new PaymentMethodDetail
                            {
                                TotalRevenue = cashRevenue,
                                TransactionCount = cashCount,
                                Percentage = (decimal)Math.Round((double)cashPercentage, 2),
                                AverageTransactionValue = (decimal)Math.Round((double)cashAverage, 2)
                            }
                        },
                        Summary = new RevenueSummary
                        {
                            TotalRevenue = totalRevenue,
                            TotalTransactions = totalCount,
                            AverageTransactionValue = (decimal)Math.Round((double)totalAverage, 2)
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
                return new PaymentMethodRevenueResponse
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        private (DateTime StartDate, DateTime EndDate) CalculateDateRange(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-12);
            var end = endDate ?? DateTime.Today;

            return (start, end);
        }
    }
}
