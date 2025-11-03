using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class RevenueReportService : IRevenueReportService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<RevenueReportService> _logger;

        public RevenueReportService(
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository,
            ILogger<RevenueReportService> logger)
        {
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<RevenueReportResponse> GetRevenueReportAsync(int centerId, RevenueReportRequest request)
        {
            try
            {
                // Lấy tất cả booking của center
                var allCenterBookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId, page: 1, pageSize: int.MaxValue, status: null);

                // Map bookingId -> booking để lấy service/technician cho groupBy
                var bookingById = allCenterBookings.ToDictionary(b => b.BookingId, b => b);

                // Thu thập payments SUCCESS theo PaidAt trong range
                var bookingPaymentAmounts = new List<(DateTime paidAt, int bookingId, decimal amount)>();
                foreach (var booking in allCenterBookings)
                {
                    var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
                    if (invoice == null) continue;
                    var payments = await _paymentRepository.GetByInvoiceIdAsync(invoice.InvoiceId, status: "COMPLETED", method: null, from: request.StartDate, to: request.EndDate);
                    foreach (var p in payments)
                    {
                        if (p.PaidAt.HasValue)
                        {
                            bookingPaymentAmounts.Add((p.PaidAt.Value, booking.BookingId, p.Amount));
                        }
                    }
                }

                // Tính toán doanh thu theo từng khoảng thời gian dựa trên PaidAt
                var revenueByPeriod = CalculateRevenueByPeriodFromPayments(bookingPaymentAmounts, request.Period);

                // Tính summary
                var summary = CalculateSummaryFromPayments(revenueByPeriod);

                // Phân nhóm dữ liệu theo service/technician bằng tổng amount theo booking
                var groupedData = CalculateGroupedDataFromPayments(bookingPaymentAmounts, bookingById, request.GroupBy);

                // Tính alerts
                var alerts = await CalculateAlertsAsync(centerId, revenueByPeriod, request.StartDate, request.EndDate);

                // Tính trends
                var trends = CalculateTrends(revenueByPeriod);

                // So sánh với kỳ trước nếu được yêu cầu
                RevenueComparison? comparison = null;
                if (request.CompareWithPrevious)
                {
                    comparison = await CalculateComparisonAsync(centerId, request.StartDate, request.EndDate);
                }

                return new RevenueReportResponse
                {
                    Summary = summary,
                    RevenueByPeriod = revenueByPeriod,
                    GroupedData = groupedData,
                    Alerts = alerts,
                    Trends = trends,
                    Comparison = comparison
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo báo cáo doanh thu cho center {CenterId}", centerId);
                throw;
            }
        }

        // New calculations based on PaidAt payments
        private List<RevenueByPeriod> CalculateRevenueByPeriodFromPayments(List<(DateTime paidAt, int bookingId, decimal amount)> bookingPayments, string period)
        {
            var result = bookingPayments
                .GroupBy(x => GetPeriodKey(x.paidAt, period))
                .Select(g => new RevenueByPeriod
                {
                    Period = g.Key,
                    Revenue = g.Sum(x => x.amount),
                    Bookings = g.Select(x => x.bookingId).Distinct().Count(),
                    Services = g.Sum(x => x.amount),
                    Parts = 0
                })
                .OrderBy(r => r.Period)
                .ToList();
            return result;
        }

        private RevenueSummary CalculateSummaryFromPayments(List<RevenueByPeriod> revenueByPeriod)
        {
            var totalRevenue = revenueByPeriod.Sum(r => r.Revenue);
            var totalBookings = revenueByPeriod.Sum(r => r.Bookings);
            var averageRevenuePerBooking = totalBookings > 0 ? totalRevenue / totalBookings : 0;
            return new RevenueSummary
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                AverageRevenuePerBooking = averageRevenuePerBooking,
                GrowthRate = "+15%",
                AlertLevel = "normal"
            };
        }

        private string GetPeriodKey(DateTime date, string period)
        {
            return period.ToLower() switch
            {
                "daily" => date.ToString("yyyy-MM-dd"),
                "weekly" => GetWeekKey(date),
                "monthly" => date.ToString("yyyy-MM"),
                "quarterly" => GetQuarterKey(date),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private string GetWeekKey(DateTime date)
        {
            var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
            return startOfWeek.ToString("yyyy-MM-dd");
        }

        private string GetQuarterKey(DateTime date)
        {
            var quarter = (date.Month - 1) / 3 + 1;
            return $"{date.Year}-Q{quarter}";
        }

        private RevenueSummary CalculateSummary(List<RevenueByPeriod> revenueByPeriod, List<Booking> bookings)
        {
            var totalRevenue = revenueByPeriod.Sum(r => r.Revenue);
            var totalBookings = revenueByPeriod.Sum(r => r.Bookings);
            var averageRevenuePerBooking = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            return new RevenueSummary
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                AverageRevenuePerBooking = averageRevenuePerBooking,
                GrowthRate = "+15%", // TODO: Calculate actual growth rate
                AlertLevel = "normal"
            };
        }

        private GroupedRevenueData CalculateGroupedDataFromPayments(List<(DateTime paidAt, int bookingId, decimal amount)> bookingPayments, Dictionary<int, Booking> bookingById, string groupBy)
        {
            var result = new GroupedRevenueData();
            var bookingAmount = bookingPayments
                .GroupBy(x => x.bookingId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.amount));

            if (groupBy == "service")
            {
                var data = bookingAmount
                    .Select(kv => new { bookingId = kv.Key, amount = kv.Value, booking = bookingById.GetValueOrDefault(kv.Key) })
                    .Where(x => x.booking != null)
                    .GroupBy(x => x.booking!.ServiceId)
                    .Select(g => new ServiceRevenue
                    {
                        ServiceId = g.Key,
                        ServiceName = g.First().booking!.Service?.ServiceName ?? "Unknown",
                        Revenue = g.Sum(x => x.amount),
                        Bookings = g.Count(),
                        Percentage = 0
                    })
                    .ToList();
                result.ByService = data;
            }

            if (groupBy == "technician")
            {
                var data = bookingAmount
                    .Select(kv => new { bookingId = kv.Key, amount = kv.Value, booking = bookingById.GetValueOrDefault(kv.Key) })
                    .Where(x => x.booking != null && x.booking.TechnicianSlotId.HasValue)
                    .GroupBy(x => x.booking!.TechnicianTimeSlot?.TechnicianId ?? 0)
                    .Select(g => new TechnicianRevenue
                    {
                        TechnicianId = g.Key,
                        TechnicianName = g.First().booking!.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Unknown",
                        Revenue = g.Sum(x => x.amount),
                        Bookings = g.Count(),
                        AverageRating = 4.5
                    })
                    .ToList();
                result.ByTechnician = data;
            }

            return result;
        }

        private Task<List<RevenueAlert>> CalculateAlertsAsync(int centerId, List<RevenueByPeriod> revenueByPeriod, DateTime startDate, DateTime endDate)
        {
            var alerts = new List<RevenueAlert>();

            // TODO: Implement alert logic
            // Check for revenue drops, no bookings, etc.

            return Task.FromResult(alerts);
        }

        private RevenueTrends CalculateTrends(List<RevenueByPeriod> revenueByPeriod)
        {
            if (!revenueByPeriod.Any())
            {
                return new RevenueTrends();
            }

            var revenues = revenueByPeriod.Select(r => r.Revenue).ToList();
            var isIncreasing = revenues.Count > 1 && revenues.Last() > revenues.First();
            var volatility = CalculateVolatility(revenues);

            return new RevenueTrends
            {
                Direction = isIncreasing ? "increasing" : "decreasing",
                Volatility = volatility,
                PeakDay = revenueByPeriod.OrderByDescending(r => r.Revenue).First().Period,
                LowestDay = revenueByPeriod.OrderBy(r => r.Revenue).First().Period
            };
        }

        private string CalculateVolatility(List<decimal> revenues)
        {
            if (revenues.Count < 2) return "low";

            var average = revenues.Average();
            var variance = revenues.Sum(r => Math.Pow((double)(r - average), 2)) / revenues.Count;
            var standardDeviation = Math.Sqrt(variance);

            return standardDeviation switch
            {
                < 100000 => "low",
                < 500000 => "medium",
                _ => "high"
            };
        }

        private Task<RevenueComparison> CalculateComparisonAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            var periodLength = endDate - startDate;
            var previousStartDate = startDate - periodLength;
            var previousEndDate = startDate;

            // TODO: Get previous period data and calculate comparison
            return Task.FromResult(new RevenueComparison
            {
                PreviousPeriod = $"{previousStartDate:yyyy-MM-dd} to {previousEndDate:yyyy-MM-dd}",
                RevenueChange = 0,
                PercentageChange = "+0%",
                BookingChange = 0
            });
        }
    }
}
